using System.Collections.Generic;
using System.Linq;
using BitBetMatic;
using BitBetMatic.API;
using Microsoft.ML;
using Microsoft.ML.Data;

public class MLStrategy : TradingStrategyBase, ITradingStrategy
{
    private readonly PredictionEngine<CandleSequenceData, CandlePrediction> _predictionEngine;
    private readonly MLContext _mlContext;
    private readonly int _sequenceLength = 200;

    public MLStrategy()
    {
        _mlContext = new MLContext();

        // Model laden
        var loadedModel = _mlContext.Model.Load(OptimalTradeFinder.MODEL_PATH, out _);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<CandleSequenceData, CandlePrediction>(loadedModel);
    }

    public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<FlaggedQuote> quotes, decimal currentPrice)
    {
        if (quotes.Count < _sequenceLength)
            return (BuySellHold.Hold, 0); // Niet genoeg data

        // Alleen de laatste N candles
        var recent = quotes.Skip(quotes.Count - _sequenceLength).ToList();

        var input = MLTradeModelSequenceTrainer.CreateCandleSequenceDataFromQuotes(recent, Thresholds, null);
        // Input vector opbouwen

        var prediction = _predictionEngine.Predict(input);
        var label = (BuySellHold)prediction.PredictedLabel;
        int score = (int)(prediction.Score?.Max() ?? 0);

        // Console.WriteLine($"Voorspelling: {prediction.PredictedLabel}, scores: {string.Join(",", prediction.Score ?? new float[0])}");

        return (label, 1000);
    }

    public override string Interval() => "15m";

    public override int Limit() => Thresholds.SmaLongTerm; // Gebruik de Thresholds waarde voor limiet
}
public class CandlePrediction
{
    [ColumnName("PredictedLabel")]
    public double PredictedLabel { get; set; }

    // Score-array voor classificatie: index 0 = class 1 (Buy), 1 = class 2 (Sell), 2 = class 3 (Hold)
    [ColumnName("Score")]
    public float[] Score { get; set; }
}