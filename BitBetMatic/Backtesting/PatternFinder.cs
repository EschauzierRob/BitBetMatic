using System;
using System.Collections.Generic;
using System.Linq;
using Skender.Stock.Indicators;


public static class CandleExtensions
{
    // Methode om pieken en dalen te vinden
    public static (List<Quote> Peaks, List<Quote> Troughs) FindPeaksAndTroughs(IEnumerable<Quote> candles)
    {
        List<Quote> peaks = new List<Quote>();
        List<Quote> troughs = new List<Quote>();

        var candleList = candles.ToList();
        if (candleList.Count < 3)
            return (peaks, troughs);

        for (int i = 1; i < candleList.Count - 1; i++)
        {
            var previous = candleList[i - 1];
            var current = candleList[i];
            var next = candleList[i + 1];

            if (current.High > previous.High && current.High > next.High)
            {
                peaks.Add(current);
            }

            if (current.Low < previous.Low && current.Low < next.Low)
            {
                troughs.Add(current);
            }
        }

        return (peaks, troughs);
    }

    // Methode om sterke stijgingen en dalingen te vinden over een bepaald venster
    public static List<(DateTime StartDate, DateTime EndDate, string TrendType, decimal PercentageChange)> FindStrongTrends(
        IEnumerable<Quote> candles, int windowSize, decimal threshold)
    {
        List<(DateTime, DateTime, string, decimal)> trends = new List<(DateTime, DateTime, string, decimal)>();

        var candleList = candles.ToList();
        if (candleList.Count < windowSize)
            return trends;

        for (int i = 0; i <= candleList.Count - windowSize; i++)
        {
            var window = candleList.Skip(i).Take(windowSize).ToList();
            var firstClose = window.First().Close;
            var lastClose = window.Last().Close;

            // Bereken procentuele verandering
            decimal percentageChange = ((lastClose - firstClose) / firstClose) * 100;

            if (percentageChange > threshold)
            {
                trends.Add((window.First().Date, window.Last().Date, "Uptrend", percentageChange));
            }
            else if (percentageChange < -threshold)
            {
                trends.Add((window.First().Date, window.Last().Date, "Downtrend", percentageChange));
            }
        }

        return trends;
    }

    // Methode om patronen te classificeren
    public static List<(string PatternType, DateTime PatternDate, List<Quote> PreReversalCandles)> ClassifyPatterns(
        IEnumerable<Quote> candles, int trendWindowSize, decimal trendThreshold, int preReversalCandleCount = 3)
    {
        var patterns = new List<(string, DateTime, List<Quote>)>();

        // Vind pieken, dalen en trends
        var (peaks, troughs) = FindPeaksAndTroughs(candles);
        var trends = FindStrongTrends(candles, trendWindowSize, trendThreshold);

        // Loop door de trends om patronen te identificeren
        for (int i = 1; i < trends.Count; i++)
        {
            var previousTrend = trends[i - 1];
            var currentTrend = trends[i];

            // Verzamel candles vóór de huidige trend
            var preReversalCandles = candles.Where(c => c.Date < currentTrend.StartDate)
                                            .OrderByDescending(c => c.Date)
                                            .Take(preReversalCandleCount)
                                            .OrderBy(c => c.Date)
                                            .ToList();

            if (currentTrend.TrendType == "Uptrend")
            {
                // Zoek naar pieken in deze uptrend
                var peakInUptrend = peaks.FirstOrDefault(p => p.Date >= currentTrend.StartDate && p.Date <= currentTrend.EndDate);
                if (peakInUptrend != null)
                {
                    patterns.Add(("Peak After Uptrend (PAU)", peakInUptrend.Date, preReversalCandles));
                }
            }
            else if (currentTrend.TrendType == "Downtrend")
            {
                // Zoek naar dalen in deze downtrend
                var troughInDowntrend = troughs.FirstOrDefault(t => t.Date >= currentTrend.StartDate && t.Date <= currentTrend.EndDate);
                if (troughInDowntrend != null)
                {
                    patterns.Add(("Trough After Downtrend (TAD)", troughInDowntrend.Date, preReversalCandles));
                }
            }

            // Detecteer omslagpunten na trends
            if (previousTrend.TrendType == "Uptrend" && currentTrend.TrendType == "Downtrend")
            {
                patterns.Add(("Reversal Down After Uptrend (RDAU)", currentTrend.StartDate, preReversalCandles));
            }
            else if (previousTrend.TrendType == "Downtrend" && currentTrend.TrendType == "Uptrend")
            {
                patterns.Add(("Reversal Up After Downtrend (RUAD)", currentTrend.StartDate, preReversalCandles));
            }
        }

        return patterns;
    }

    // Methode om candlestick-patronen te analyseren in de aanloop naar een omslagpunt
    public static List<string> AnalyzePreReversalCandles(List<Quote> preReversalCandles)
    {
        var behaviors = new List<string>();

        if (preReversalCandles == null || preReversalCandles.Count == 0)
            return behaviors;

        // Controleer op specifieke gedragingen, zoals kleine bodies, dalend volume, enz.
        bool narrowingCandles = preReversalCandles.Zip(preReversalCandles.Skip(1), (a, b) => BodySize(a) > BodySize(b)).All(x => x);
        bool increasingVolume = preReversalCandles.Zip(preReversalCandles.Skip(1), (a, b) => a.Volume < b.Volume).All(x => x);
        bool decreasingVolume = preReversalCandles.Zip(preReversalCandles.Skip(1), (a, b) => a.Volume > b.Volume).All(x => x);

        if (narrowingCandles)
            behaviors.Add("Narrowing candle bodies detected");

        if (increasingVolume)
            behaviors.Add("Increasing volume detected");

        if (decreasingVolume)
            behaviors.Add("Decreasing volume detected");

        // Voeg meer patronen of analyses toe zoals Doji, Hammer, Engulfing etc.
        foreach (var candle in preReversalCandles)
        {
            if (IsDoji(candle))
                behaviors.Add($"Doji pattern on {candle.Date}");
            if (IsHammer(candle))
                behaviors.Add($"Hammer pattern on {candle.Date}");
            if (IsEngulfing(preReversalCandles))
                behaviors.Add($"Engulfing pattern detected before reversal");
        }

        return behaviors;
    }

    // Hulpmethodes om candlestick-patronen te herkennen
    private static bool IsDoji(Quote candle)
    {
        return Math.Abs(candle.Open - candle.Close) < (candle.High - candle.Low) * 0.1m; // 10% regel voor Doji
    }

    private static bool IsHammer(Quote candle)
    {
        return BodySize(candle) < (candle.High - candle.Low) * 0.3m && (candle.Close > candle.Open ? candle.Low : candle.High) < candle.Low + (candle.High - candle.Low) * 0.2m;
    }

    private static bool IsEngulfing(List<Quote> candles)
    {
        if (candles.Count < 2) return false;
        var last = candles[candles.Count - 1];
        var secondLast = candles[candles.Count - 2];
        return BodySize(last) > BodySize(secondLast) && ((last.Close > last.Open && secondLast.Close < secondLast.Open) || (last.Close < last.Open && secondLast.Close > secondLast.Open));
    }



    // Bereken de body size van een candle
    public static decimal BodySize(Quote quote) => Math.Abs(quote.Close - quote.Open);

    // Bepaal het type van de candle (bullish of bearish)
    public static string Type(Quote quote) => quote.Close >= quote.Open ? "Bullish" : "Bearish";
}

public static class ReversalAnalysis
{
    // Bereken indicatorwaarden en analyseer reversals
    public static void AnalyzeReversalsWithIndicators(IEnumerable<Quote> quotes)
    {
        // Bereken indicatoren
        var atr = quotes.GetAtr(14).ToList();
        var ema200 = quotes.GetEma(200).ToList();
        var ema50 = quotes.GetEma(50).ToList();
        var rsi = quotes.GetRsi(14).ToList();
        var macd = quotes.GetMacd().ToList();
        var bb = quotes.GetBollingerBands(20, 2).ToList();
        var adx = quotes.GetAdx(14).ToList();
        var psar = quotes.GetParabolicSar().ToList();
        var stochastic = quotes.GetStoch(14, 3, 3).ToList();
        var roc = quotes.GetRoc(12).ToList();

        // Identificeer reversal-patronen met de bijbehorende indicatorwaarden
        List<ReversalPattern> reversalPatterns = new List<ReversalPattern>();

        for (int i = 1; i < quotes.Count(); i++)
        {
            // Controleer of er een reversal is (bijvoorbeeld via prijsveranderingen of andere logica)
            if (IsReversal(quotes.ElementAt(i - 1), quotes.ElementAt(i)))
            {
                // Verzamel indicatorwaarden op het moment van de reversal
                var pattern = new ReversalPattern
                {
                    Date = quotes.ElementAt(i).Date,
                    AtrValue = atr.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Atr ?? 0,
                    Ema200Value = ema200.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Ema ?? 0,
                    Ema50Value = ema50.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Ema ?? 0,
                    RsiValue = rsi.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Rsi ?? 0,
                    MacdValue = macd.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Macd ?? 0,
                    MacdSignalValue = macd.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Signal ?? 0,
                    BbUpper = bb.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.UpperBand ?? 0,
                    BbLower = bb.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.LowerBand ?? 0,
                    AdxValue = adx.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Adx ?? 0,
                    PsarValue = psar.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Sar ?? 0,
                    StochasticValue = stochastic.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.K ?? 0,
                    RocValue = roc.FirstOrDefault(x => x.Date == quotes.ElementAt(i).Date)?.Roc ?? 0
                };
                
                reversalPatterns.Add(pattern);
            }
        }

        // Analyseer de verzamelde reversal patronen en hun indicatorwaarden
        Console.WriteLine("Reversal Patronen en hun Indicatorwaarden:");
        foreach (var pattern in reversalPatterns)
        {
            Console.WriteLine($"Datum: {pattern.Date}, ATR: {pattern.AtrValue}, EMA200: {pattern.Ema200Value}, EMA50: {pattern.Ema50Value}, RSI: {pattern.RsiValue}, MACD: {pattern.MacdValue}, Stochastic: {pattern.StochasticValue}, ROC: {pattern.RocValue}");
        }
    }

    // Hulpmethode om een reversal te identificeren (bijv. op basis van candlepatronen)
    private static bool IsReversal(Quote prevQuote, Quote currentQuote)
    {
        // Placeholder voor eigen logica om te bepalen of er een reversal is
        return currentQuote.Close < prevQuote.Close && prevQuote.Close > prevQuote.Open; // Simpele voorbeeldregel
    }
}

// Klasse om reversal-patronen met hun indicatorwaarden bij te houden
public class ReversalPattern
{
    public DateTime Date { get; set; }
    public double AtrValue { get; set; }
    public double Ema200Value { get; set; }
    public double Ema50Value { get; set; }
    public double RsiValue { get; set; }
    public double MacdValue { get; set; }
    public double MacdSignalValue { get; set; }
    public double BbUpper { get; set; }
    public double BbLower { get; set; }
    public double AdxValue { get; set; }
    public double PsarValue { get; set; }
    public double StochasticValue { get; set; }
    public double RocValue { get; set; }
}
