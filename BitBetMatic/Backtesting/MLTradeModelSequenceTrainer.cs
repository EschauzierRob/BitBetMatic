using System;
using System.Collections.Generic;
using System.Linq;
using BitBetMatic.API;
using Microsoft.ML;
using Microsoft.ML.Data;
using Skender.Stock.Indicators;

public class CandleData
{
    public float Open;
    public float High;
    public float Low;
    public float Close;
    public float Volume;
}

// Deze klasse representeert één input-voorbeeld voor het model, met een featurevector (bijv. 96 candles * 5 features = 480) en een label.
public class CandleSequenceData
{
    [VectorType(490)] // 480 + 10
    public float[] Features { get; set; }

    // Label: 1 = Buy, 2 = Sell, 3 = Hold
    public double Label { get; set; }
}

public class TradePrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedTradeAction; // 1 = Buy, 2 = Sell, 3 = Hold
}
public class MLTradeModelSequenceTrainer
{
    private const int LookbackCount = 200;
    private const int FeatureCount = 5;
    private MLContext mlContext;

    public MLTradeModelSequenceTrainer()
    {
        mlContext = new MLContext(seed: 1);
    }

    public static CandleSequenceData CreateCandleSequenceDataFromQuotes(List<FlaggedQuote> quotes, IndicatorThresholds thresholds, uint? label)
    {
        // Zorg dat je genoeg candles hebt
        int requiredCandles = Math.Max(
            Math.Max(thresholds.SmaLongTerm, thresholds.MacdSlowPeriod + thresholds.MacdSignalPeriod),
            Math.Max(thresholds.BollingerBandsPeriod, thresholds.AdxPeriod)
        );
        if (quotes.Count < requiredCandles)
            throw new ArgumentException($"Niet genoeg candles. Minimaal nodig: {requiredCandles}, ontvangen: {quotes.Count}");

        // 480-delige feature vector maken (bijv. 96 candles × 5 features)
        var recentQuotes = quotes.TakeLast(96).ToList();
        float[] baseFeatures = recentQuotes.SelectMany(q => new float[]
        {
            (float)q.Open,
            (float)q.High,
            (float)q.Low,
            (float)q.Close,
            (float)q.Volume
        }).ToArray();

        float Safe(float? value) => value ?? 0f;

        var ema50 = Safe((float)quotes.GetEma(thresholds.SmaShortTerm).LastOrDefault()?.Ema);
        var ema200 = Safe((float)quotes.GetEma(thresholds.SmaLongTerm).LastOrDefault()?.Ema);

        var rsi = Safe((float)quotes.GetRsi(thresholds.RsiPeriod).LastOrDefault()?.Rsi);

        var macdResult = quotes.GetMacd(thresholds.MacdFastPeriod, thresholds.MacdSlowPeriod, thresholds.MacdSignalPeriod).LastOrDefault();
        var macd = Safe((float)macdResult?.Macd);
        var macdSignal = Safe((float)macdResult?.Signal);

        var bbResult = quotes.GetBollingerBands(thresholds.BollingerBandsPeriod, thresholds.BollingerBandsDeviation).LastOrDefault();
        var bbUpper = Safe((float)bbResult?.UpperBand);
        var bbLower = Safe((float)bbResult?.LowerBand);

        var stochResult = quotes.GetStoch(thresholds.StochasticPeriod, thresholds.StochasticSignalPeriod).LastOrDefault();
        var stochK = Safe((float)stochResult?.Oscillator);
        var stochD = Safe((float)stochResult?.Signal);

        var adx = Safe((float)quotes.GetAdx(thresholds.AdxPeriod).LastOrDefault()?.Adx);

        float[] extraFeatures =
        {
            ema50, ema200, rsi, macd, macdSignal,
            bbUpper, bbLower, stochK, stochD, adx
        };
        float[] fullFeatureVector = baseFeatures.Concat(extraFeatures).ToArray();

        var sequenceData = new CandleSequenceData
        {
            Features = fullFeatureVector,
        };

        if (label.HasValue)
        {
            sequenceData.Label = label.Value;
        }

        return sequenceData;
    }





    // Maakt een enkele featurevector
    public CandleSequenceData PrepareInput(List<CandleData> candles, uint label)
    {
        if (candles.Count != LookbackCount)
            throw new ArgumentException($"Precies {LookbackCount} candles vereist, kreeg {candles.Count}");

        float[] features = new float[LookbackCount * FeatureCount];
        int idx = 0;
        foreach (var candle in candles)
        {
            features[idx++] = candle.Open;
            features[idx++] = candle.High;
            features[idx++] = candle.Low;
            features[idx++] = candle.Close;
            features[idx++] = candle.Volume;
        }

        return new CandleSequenceData { Features = features, Label = label };
    }

    public IDataView PrepareFromFlaggedQuotes(List<FlaggedQuote> allQuotes)
    {
        var data = new List<CandleSequenceData>();

        for (int i = LookbackCount; i < allQuotes.Count; i++)
        {
            var labelQuote = allQuotes[i];
            uint label = (uint)labelQuote.TradeAction;

            // Sla over als label niet bruikbaar is
            if (label < 1 || label > 3) continue;

            var window = allQuotes.GetRange(i - LookbackCount, LookbackCount).ToList();

            var thresholds = new IndicatorThresholds();

            var entry = CreateCandleSequenceDataFromQuotes(window, thresholds, label);
            data.Add(entry);
        }

        return mlContext.Data.LoadFromEnumerable(data);
    }

    // Hier nemen we een complete lijst met sequenties + labels uit je OptimalTradeFinder
    // public IDataView LoadDataFromFinder(List<(List<CandleData> sequence, uint label)> sequences)
    // {
    //     var data = new List<CandleSequenceData>();
    //     foreach (var (sequence, label) in sequences)
    //     {
    //         if (sequence.Count == LookbackCount)
    //             data.Add(PrepareInput(sequence, label));
    //     }
    //     return mlContext.Data.LoadFromEnumerable(data);
    // }
    public IDataView CombineDatasets(IDataView first, IDataView second)
    {
        var firstEnum = mlContext.Data.CreateEnumerable<CandleSequenceData>(first, reuseRowObject: false);
        var secondEnum = mlContext.Data.CreateEnumerable<CandleSequenceData>(second, reuseRowObject: false);

        var combined = firstEnum.Concat(secondEnum);

        return mlContext.Data.LoadFromEnumerable(combined);
    }

    private IDataView SplitData(IDataView dataView)
    {
        // ➤ Stap 1: splits per klasse
        var buyData = mlContext.Data.FilterRowsByColumn(dataView, "Label", lowerBound: 1, upperBound: 1.1);
        var sellData = mlContext.Data.FilterRowsByColumn(dataView, "Label", lowerBound: 2, upperBound: 2.1);
        var holdData = mlContext.Data.FilterRowsByColumn(dataView, "Label", lowerBound: 3, upperBound: 3.1);

        // ➤ Stap 2: equalize / undersample Hold-klasse
        // Pas hier eventueel het aantal aan
        var buyCount = mlContext.Data.CreateEnumerable<CandleSequenceData>(buyData, reuseRowObject: false).Count();
        var sellCount = mlContext.Data.CreateEnumerable<CandleSequenceData>(sellData, reuseRowObject: false).Count();
        int minClassCount = Math.Min(buyCount, sellCount);
        var holdSampled = mlContext.Data.TakeRows(holdData, minClassCount);

        // ➤ Stap 3: samenvoegen tot gebalanceerde set

        var balancedData = CombineDatasets(sellData, buyData);
        balancedData = CombineDatasets(balancedData, holdSampled);
        return balancedData;
    }

    public void TrainModel(IDataView dataView, string modelPath)
    {
        var labelColumn = dataView.Schema["Label"];
        Console.WriteLine($"Label type: {labelColumn.Type}");

        dataView = mlContext.Transforms.Conversion.ConvertType("Label", outputKind: DataKind.Double)
            .Fit(dataView)
            .Transform(dataView);

        var balancedData = dataView;
        // var balancedData = SplitData(dataView);

        var split = mlContext.Data.TrainTestSplit(balancedData, testFraction: 0.2);

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"))
            .Append(mlContext.Transforms.NormalizeMinMax("Features"));

        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");

        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:F2}");
        Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:F2}");
        printLabelSplit(split);

        mlContext.Model.Save(model, dataView.Schema, modelPath);
        Console.WriteLine($"Model saved to {modelPath}");
    }

    private void printLabelSplit(DataOperationsCatalog.TrainTestData split)
    {
        // Toon labelverdeling (histogram)
        var labelDistribution = mlContext.Data.CreateEnumerable<CandleSequenceData>(split.TrainSet, reuseRowObject: false)
            .GroupBy(d => d.Label)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderBy(x => x.Label);

        Console.WriteLine("Labelverdeling in trainingset:");
        foreach (var entry in labelDistribution)
        {
            string labelName = entry.Label switch
            {
                1 => "Buy",
                2 => "Sell",
                3 => "Hold",
                _ => "Onbekend"
            };
            Console.WriteLine($"  {labelName} ({entry.Label}): {entry.Count}");
        }

    }
}