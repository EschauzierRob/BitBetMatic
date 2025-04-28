using System;
using System.Collections.Generic;
using System.Linq;
using BitBetMatic.API;
using Microsoft.ML;
using Microsoft.ML.Data;

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
    [VectorType(480)]
    public float[] Features { get; set; }

    // Label: 1 = Buy, 2 = Sell, 3 = Hold
    public uint Label { get; set; }
}

public class TradePrediction
{
    [ColumnName("PredictedLabel")]
    public uint PredictedTradeAction; // 1 = Buy, 2 = Sell, 3 = Hold
}
public class MLTradeModelSequenceTrainer
{
    private const int LookbackCount = 96;
    private const int FeatureCount = 5;
    private MLContext mlContext;

    public MLTradeModelSequenceTrainer()
    {
        mlContext = new MLContext(seed: 1);
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

            var window = allQuotes.GetRange(i - LookbackCount, LookbackCount)
                .Select(q => new CandleData
                {
                    Open = (float)q.Open,
                    High = (float)q.High,
                    Low = (float)q.Low,
                    Close = (float)q.Close,
                    Volume = (float)q.Volume
                }).ToList();

            var entry = PrepareInput(window, label);
            data.Add(entry);
        }

        return mlContext.Data.LoadFromEnumerable(data);
    }


    // Hier nemen we een complete lijst met sequenties + labels uit je OptimalTradeFinder
    public IDataView LoadDataFromFinder(List<(List<CandleData> sequence, uint label)> sequences)
    {
        var data = new List<CandleSequenceData>();
        foreach (var (sequence, label) in sequences)
        {
            if (sequence.Count == LookbackCount)
                data.Add(PrepareInput(sequence, label));
        }
        return mlContext.Data.LoadFromEnumerable(data);
    }

    public void TrainModel(IDataView dataView, string modelPath)
    {
        var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");

        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:F2}");
        Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:F2}");

        mlContext.Model.Save(model, dataView.Schema, modelPath);
        Console.WriteLine($"Model saved to {modelPath}");
    }
}