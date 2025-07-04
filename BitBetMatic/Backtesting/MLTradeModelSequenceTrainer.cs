using System;
using System.Collections.Generic;
using System.Linq;
using BitBetMatic.API;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
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
    private readonly MLContext MlContext;

    public MLTradeModelSequenceTrainer()
    {
        MlContext = new MLContext(seed: 1);
    }
    public static CandleSequenceData CreateCandleSequenceDataFromQuotes(List<FlaggedQuote> quotes, IndicatorThresholds thresholds, uint? label)
    {
        int requiredCandles = Math.Max(
            Math.Max(thresholds.SmaLongTerm, thresholds.MacdSlowPeriod + thresholds.MacdSignalPeriod),
            Math.Max(thresholds.BollingerBandsPeriod, thresholds.AdxPeriod)
        );
        if (quotes.Count < requiredCandles)
            throw new ArgumentException($"Niet genoeg candles. Minimaal nodig: {requiredCandles}, ontvangen: {quotes.Count}");

        var recentQuotes = quotes.TakeLast(96).ToList();

        // Gebruik de eerste Close als referentie voor prijs-normalisatie
        double baseClose = (double)recentQuotes.First().Close;
        double avgVolume = (double)recentQuotes.Average(q => q.Volume);

        float Safe(float? value) => value ?? 0f;

        // Genormaliseerde candle features: relatieve bewegingen i.p.v. absolute prijzen
        float[] baseFeatures = recentQuotes.SelectMany(q =>
        {
            float relOpen = (float)(((double)q.Open - baseClose) / baseClose);
            float relHigh = (float)((q.High - q.Open) / q.Open);       // Hoogte t.o.v. open
            float relLow = (float)((q.Low - q.Open) / q.Open);         // Diepte t.o.v. open
            float relClose = (float)((q.Close - q.Open) / q.Open);     // Body%
            float relVolume = (float)((double)q.Volume / avgVolume);          // Volume als % van gemiddelde

            return new float[] { relOpen, relHigh, relLow, relClose, relVolume };
        }).ToArray();

        float ema50 = Safe((float)quotes.GetEma(thresholds.SmaShortTerm).LastOrDefault()?.Ema);
        float ema200 = Safe((float)quotes.GetEma(thresholds.SmaLongTerm).LastOrDefault()?.Ema);
        float rsi = Safe((float)quotes.GetRsi(thresholds.RsiPeriod).LastOrDefault()?.Rsi);

        var macdResult = quotes.GetMacd(thresholds.MacdFastPeriod, thresholds.MacdSlowPeriod, thresholds.MacdSignalPeriod).LastOrDefault();
        float macd = Safe((float)macdResult?.Macd);
        float macdSignal = Safe((float)macdResult?.Signal);

        var bbResult = quotes.GetBollingerBands(thresholds.BollingerBandsPeriod, thresholds.BollingerBandsDeviation).LastOrDefault();
        float bbUpper = Safe((float)bbResult?.UpperBand);
        float bbLower = Safe((float)bbResult?.LowerBand);

        var stochResult = quotes.GetStoch(thresholds.StochasticPeriod, thresholds.StochasticSignalPeriod).LastOrDefault();
        float stochK = Safe((float)stochResult?.Oscillator);
        float stochD = Safe((float)stochResult?.Signal);

        float adx = Safe((float)quotes.GetAdx(thresholds.AdxPeriod).LastOrDefault()?.Adx);

        // Relatieve indicatorfeatures
        float latestClose = (float)recentQuotes.Last().Close;
        float[] extraFeatures =
        {
            (ema50 - latestClose) / latestClose,
            (ema200 - latestClose) / latestClose,
            rsi / 100f,                     // RSI tussen 0-1
            macd / latestClose,
            macdSignal / latestClose,
            (bbUpper - latestClose) / latestClose,
            (bbLower - latestClose) / latestClose,
            stochK / 100f,
            stochD / 100f,
            adx / 100f
        };

        float[] fullFeatureVector = baseFeatures.Concat(extraFeatures).ToArray();

        var sequenceData = new CandleSequenceData
        {
            Features = fullFeatureVector,
        };

        if (label.HasValue)
            sequenceData.Label = label.Value;

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

        return MlContext.Data.LoadFromEnumerable(data);
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
        var firstEnum = MlContext.Data.CreateEnumerable<CandleSequenceData>(first, reuseRowObject: false);
        var secondEnum = MlContext.Data.CreateEnumerable<CandleSequenceData>(second, reuseRowObject: false);

        var combined = firstEnum.Concat(secondEnum);

        return MlContext.Data.LoadFromEnumerable(combined);
    }

    private IDataView SplitData(IDataView dataView)
    {
        // ➤ Stap 1: splits per klasse
        var buyData = MlContext.Data.FilterRowsByColumn(dataView, "Label", lowerBound: 1, upperBound: 1.1);
        var sellData = MlContext.Data.FilterRowsByColumn(dataView, "Label", lowerBound: 2, upperBound: 2.1);
        var holdData = MlContext.Data.FilterRowsByColumn(dataView, "Label", lowerBound: 3, upperBound: 3.1);

        // ➤ Stap 2: equalize / undersample Hold-klasse
        // Pas hier eventueel het aantal aan
        var buyCount = MlContext.Data.CreateEnumerable<CandleSequenceData>(buyData, reuseRowObject: false).Count();
        var sellCount = MlContext.Data.CreateEnumerable<CandleSequenceData>(sellData, reuseRowObject: false).Count();
        int minClassCount = Math.Min(buyCount, sellCount);
        var holdSampled = MlContext.Data.TakeRows(holdData, minClassCount);

        // ➤ Stap 3: samenvoegen tot gebalanceerde set

        var balancedData = CombineDatasets(sellData, buyData);
        balancedData = CombineDatasets(balancedData, holdSampled);
        return balancedData;
    }

    public void TrainModel(IDataView dataView, string modelPath)
    {
        dataView = MlContext.Transforms.Conversion.ConvertType("Label", outputKind: DataKind.Double)
            .Fit(dataView)
            .Transform(dataView);

        // var balancedData = dataView;
        var balancedData = SplitData(dataView);

        var split = MlContext.Data.TrainTestSplit(balancedData, testFraction: 0.2);

        var options = new LightGbmMulticlassTrainer.Options
        {
            NumberOfIterations = 1000,
            LearningRate = 0.1,
            NumberOfLeaves = 31,
            MinimumExampleCountPerLeaf = 20,
            MaximumBinCountPerFeature = 255,
            UseCategoricalSplit = false,
            Booster = new GradientBooster.Options
            {
                L2Regularization = 1.0,
                L1Regularization = 0.5
            },
            EarlyStoppingRound = 50
        };

        var pipeline = MlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(MlContext.Transforms.NormalizeMinMax("Features"))
            .Append(MlContext.MulticlassClassification.Trainers.LightGbm(options))
            .Append(MlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = MlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");

        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:F2}");
        Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:F2}");
        Console.WriteLine($"ConfusionMatrix: {metrics.ConfusionMatrix:F2}");
        printLabelSplit(split);

        MlContext.Model.Save(model, dataView.Schema, modelPath);
        Console.WriteLine($"Model saved to {modelPath}");
    }

    private void printLabelSplit(DataOperationsCatalog.TrainTestData split)
    {
        // Toon labelverdeling (histogram)
        var labelDistribution = MlContext.Data.CreateEnumerable<CandleSequenceData>(split.TrainSet, reuseRowObject: false)
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