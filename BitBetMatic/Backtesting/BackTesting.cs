using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitBetMatic;
using BitBetMatic.API;
using Newtonsoft.Json;
using Skender.Stock.Indicators;

public class BackTesting
{
    private DataLoader dataLoader;
    private IndicatorThresholdPersistency indicatorThresholdPersistency;
    public const string BtcMarket = "BTC-EUR";
    public const string EthMarket = "ETH-EUR";

    private const double _maxDeviation = 50d;
    private const decimal _startingBalance = 300.0m;

    public BackTesting(IApiWrapper api)
    {
        dataLoader = new DataLoader(api);
        indicatorThresholdPersistency = new IndicatorThresholdPersistency();
    }

    private (ITradingStrategy strategy, decimal result, string resultText, Metrics metrics, TradeQuality tradeQuality) RunBacktest(TradingStrategyBase strategy, string market, List<Quote> historicalData)
    {
        var portfolioManager = new PortfolioManager();
        portfolioManager.SetCash(_startingBalance);
        var strategyExecutor = new StrategyExecutor(strategy);

        var tradeActions = strategyExecutor.ExecuteStrategy(market, historicalData, portfolioManager);

        foreach (var action in tradeActions)
        {
            portfolioManager.ExecuteTrade(action);
        }

        var resultAnalyzer = new ResultAnalyzer(strategy, tradeActions, portfolioManager);

        var analises = resultAnalyzer.Analyze();

        return (strategy, portfolioManager.GetAccountTotal(), analises.resultText, analises.metrics, analises.tradeQuality);
    }

    public async Task<(ITradingStrategy strategyBtc, ITradingStrategy strategyEth, string result)> RunBacktesting(StringBuilder sb)
    {
        sb.AppendLine("BTC backtesting:");
        var strategyBtc = await GetMostPerformantStrategy(sb, BtcMarket);

        sb.AppendLine("ETH backtesting:");
        var strategyEth = await GetMostPerformantStrategy(sb, EthMarket);

        string result = sb.ToString();
        Console.Write(result);

        return (strategyBtc, strategyEth, result);
    }
    public async Task<(TradingStrategyBase strategy, string result)> DoBacktestTuning<TStrat>(StringBuilder sb, string market, int numberOfVariants = 20) where TStrat : TradingStrategyBase, new()
    {
        var strategy = new TStrat();

        var prices = dataLoader.LoadHistoricalData(market, strategy.Interval(), 1440, DateTime.Today.AddDays(-15), DateTime.Today).Result.Select(x => (double)x.Close);
        var decayRate = VolatilityCalculator.CalculateDecayRate(prices.ToList());

        var thresholds = await indicatorThresholdPersistency.GetLatestDecayedThresholdsAsync(strategy.GetType().Name, market, decayRate) ?? strategy.Thresholds;

        if (thresholds != null)
        {
            strategy.Thresholds = thresholds;
        }

        var (strat, highscore) = await GetMostPerformantStrategyVariant(strategy, sb, market, numberOfVariants);

        if (highscore > strategy.Thresholds.Highscore)
        {
            sb.AppendLine($"SAVING NEW HIGHSCORE: {highscore} for {strat.GetType().Name} - {market}");
            strat.Thresholds.Market = market;
            strat.Thresholds.Strategy = strat.GetType().Name;
            strat.Thresholds.Highscore = highscore;
            await indicatorThresholdPersistency.InsertThresholdsAsync(strat.Thresholds);
        }

        string thresholdsWinner = JsonConvert.SerializeObject(((TStrat)strat).Thresholds);

        // sb.AppendLine("Thresholds: ");
        // sb.AppendLine(thresholdsWinner);

        string resultString = sb.ToString();
        Console.Write(resultString);

        return (strat, resultString);
    }
    public async Task<(TradingStrategyBase strategy, string result)> DoBacktestDeepTuning<TStrat>(StringBuilder sb, string market, int numberOfVariants = 20) where TStrat : TradingStrategyBase, new()
    {
        var strategy = new TStrat();

        var prices = dataLoader.LoadHistoricalData(market, strategy.Interval(), 1440, DateTime.Today.AddDays(-15), DateTime.Today).Result.Select(x => (double)x.Close);
        var decayRate = VolatilityCalculator.CalculateDecayRate(prices.ToList());

        var thresholds = await indicatorThresholdPersistency.GetLatestDecayedThresholdsAsync(strategy.GetType().Name, market, decayRate) ?? strategy.Thresholds;

        if (thresholds != null)
        {
            strategy.Thresholds = thresholds;
        }

        var (strat, highscore) = await GetMostPerformantStrategyVariant(strategy, sb, market, numberOfVariants, 50d);
        (strat, highscore) = await GetMostPerformantStrategyVariant(strat, sb, market, numberOfVariants, 25d);
        (strat, highscore) = await GetMostPerformantStrategyVariant(strat, sb, market, numberOfVariants, 10d);
        (strat, highscore) = await GetMostPerformantStrategyVariant(strat, sb, market, numberOfVariants, 5d);

        if (highscore > strategy.Thresholds.Highscore)
        {
            sb.AppendLine($"SAVING NEW HIGHSCORE: {highscore} for {strat.GetType().Name} - {market}");
            strat.Thresholds.Market = market;
            strat.Thresholds.Strategy = strat.GetType().Name;
            strat.Thresholds.Highscore = highscore;
            await indicatorThresholdPersistency.InsertThresholdsAsync(strat.Thresholds);
        }

        string thresholdsWinner = JsonConvert.SerializeObject(((TStrat)strat).Thresholds);

        // sb.AppendLine("Thresholds: ");
        // sb.AppendLine(thresholdsWinner);

        string resultString = sb.ToString();
        Console.Write(resultString);

        return (strat, resultString);
    }

    private async Task<ITradingStrategy> GetMostPerformantStrategy(StringBuilder sb, string market)
    {
        List<TradingStrategyBase> strategies = new List<TradingStrategyBase>
            {
                new ModerateStrategy(),
                new AgressiveStrategy(),
                new ScoredStrategy(),
                new StoplossStrategy(),
                new AdvancedStrategy(),
                new HoldStrategy()
                };
        (ITradingStrategy strategy, decimal total) res = (strategies.First(), decimal.Zero);
        List<Quote> historicalData = await GetHistoricalData(market);


        foreach (var strat in strategies)
        {
            var thresholds = await indicatorThresholdPersistency.GetLatestThresholdsAsync(strat.GetType().Name, market) ?? strat.Thresholds;
            strat.Thresholds = thresholds;

            var testRes = RunBacktest(strat, market, historicalData);
            sb.AppendLine(testRes.resultText);
            if (res.total < testRes.result && testRes.strategy.GetType() != new HoldStrategy().GetType())
            {
                res = (testRes.strategy, testRes.result);
            }
        }

        return res.strategy;
    }
    private async Task<List<Quote>> GetHistoricalData(string market, string interval = "1h", DateTime? start = null, DateTime? end = null)
    {
        start ??= DateTime.Today.AddDays(-60);
        end ??= DateTime.Today;
        var historicalData = await dataLoader.LoadHistoricalData(market, interval, 1440, start.Value, end.Value);
        return historicalData;
    }

    private async Task<(TStrat strategy, decimal result)> GetMostPerformantStrategyVariant<TStrat>(TStrat strategy, StringBuilder sb, string market, int numberOfVariants, double maxDeviation = _maxDeviation) where TStrat : TradingStrategyBase, new()
    {
        var thresholdVariants = GenerateThresholdVariations(strategy.Thresholds, numberOfVariants - 1, maxDeviation);
        List<TStrat> strategies = new List<TStrat> { strategy };

        foreach (var thresholdVariant in thresholdVariants)
        {
            strategies.Add(new TStrat { Thresholds = thresholdVariant });
        }

        (TradingStrategyBase strategy, decimal total) res = (strategies.First(), decimal.Zero);
        var tasks = await RunMultiRangeHistoricalTesting(market, strategies, res, 360);

        // Wacht op alle taken om te voltooien
        var results = await Task.WhenAll(tasks);

        var runs = new List<(TStrat strat, TradeQuality tradeQuality, decimal result)>();
        // Verwerk de resultaten
        foreach (var testRes in results)
        {
            runs.Add((testRes.strategy, testRes.tradeQuality, testRes.result));
            // sb.AppendLine(testRes.resultText);
        }

        // var winningMetrics = MetricsComparer.CompareMetricsWithResult(runs);
        var rankedStratResults = MetricsComparer.RankStrategiesByScore(runs);
        sb.AppendLine($"Winning variant of {rankedStratResults.Strategy.GetType().Name} - {market} got a total result of {rankedStratResults.FinalPortfolioValue:F} and a combined score of {rankedStratResults.Score}");




        Console.WriteLine($"Strategy: {rankedStratResults.Strategy}, Score: {rankedStratResults.Score:F2}, " +
                          $"Correct Trades: {rankedStratResults.TradeQuality.CorrectTradePercentage:F2}%, " +
                          $"Average Delta: {rankedStratResults.TradeQuality.AverageDelta:F2}%, " +
                          $"Final Portfolio: {rankedStratResults.FinalPortfolioValue:F2}");

        // sb.AppendLine($"Winning variant of {winningMetrics.Strat.GetType().Name} got a total result of {winningMetrics.Result:F} and a combined score of {winningMetrics.Metrics.printableMetrics}");
        // return (winningMetrics.Strat, winningMetrics.Result);
        return (rankedStratResults.Strategy, rankedStratResults.FinalPortfolioValue);
    }

    private async Task<List<Task<(TStrat strategy, decimal result, string resultText, TradeQuality tradeQuality)>>> RunMultiRangeHistoricalTesting<TStrat>(string market, List<TStrat> strategies, (TradingStrategyBase strategy, decimal total) res, int longSpan = 360) where TStrat : TradingStrategyBase, new()
    {
        // Gebruik verschillende tijdspannes
        var historicalDataLong = await GetHistoricalData(market, res.strategy.Interval(), DateTime.Today.AddDays(-longSpan));
        var historicalDataMedium = historicalDataLong.Where(x => x.Date > DateTime.Today.AddDays(-longSpan / 2)).ToList();
        var historicalDataShort = historicalDataMedium.Where(x => x.Date > DateTime.Today.AddDays(-longSpan / 12)).ToList();

        var tasks = new List<Task<(TStrat strategy, decimal result, string resultText, TradeQuality tradeQuality)>>();

        // Run backtest for different time spans
        foreach (var strat in strategies)
        {
            tasks.Add(Task.Run(() =>
            {
                // Run backtests on different time frames
                var shortTermResult = RunBacktest(strat, market, historicalDataShort);
                var mediumTermResult = RunBacktest(strat, market, historicalDataMedium);
                var longTermResult = RunBacktest(strat, market, historicalDataLong);

                // Combineer de resultaten met gewichten voor recentheid (bijv. 50% short, 30% medium, 20% long)
                decimal combinedResult = 0.5m * shortTermResult.result + 0.3m * mediumTermResult.result + 0.2m * longTermResult.result;

                // Combineer de teksten van de resultaten
                string combinedText = $"{market} - SHORT: {shortTermResult.resultText}\nMEDIUM: {mediumTermResult.resultText}\nLONG: {longTermResult.resultText}\nCombined Result: {combinedResult}";
                // Console.WriteLine(combinedText);

                return (strategy: strat, result: combinedResult, resultText: combinedText, shortTermResult.tradeQuality);
            }));
        }

        return tasks;
    }

    private async Task<List<Task<(TStrat strategy, decimal result, string resultText)>>> Run60DayHistoricalTesting<TStrat>(string market, List<TStrat> strategies, (TradingStrategyBase strategy, decimal total) res) where TStrat : TradingStrategyBase, new()
    {
        // Gebruik verschillende tijdspannes
        var historicalData = await GetHistoricalData(market, res.strategy.Interval());

        List<Task<(TStrat strategy, decimal result, string resultText)>> tasks = new List<Task<(TStrat strategy, decimal result, string resultText)>>();

        // Run backtest for different time spans
        foreach (var strat in strategies)
        {
            tasks.Add(Task.Run(() =>
            {
                var resultScore = RunBacktest(strat, market, historicalData);

                // Combineer de teksten van de resultaten
                string combinedText = $"Result: {resultScore.result}";

                return (strategy: strat, resultScore.result, resultScore.resultText);
            }));
        }

        return tasks;
    }

    public IEnumerable<IndicatorThresholds> GenerateThresholdVariations(IndicatorThresholds baseThresholds, int variationCount, double maxDeviationPercentage)
    {
        var random = new Random();

        for (int i = 0; i < variationCount; i++)
        {
            // Calculate percentage-based deviation ranges
            int GetRandomIntWithinDeviation(int baseValue, double percentage)
            {
                int deviation = (int)Math.Round(Math.Abs(baseValue) * percentage / 100.0);
                return baseValue + random.Next(-deviation, deviation + 1);
            }

            decimal GetRandomDecimalWithinDeviation(decimal baseValue, double percentage)
            {
                decimal deviation = Math.Abs(baseValue) * (decimal)percentage / 100.0m;
                return baseValue + ((decimal)random.NextDouble() * 2 * deviation - deviation);
            }

            double GetRandomDoubleWithinDeviation(double baseValue, double percentage)
            {
                double deviation = Math.Abs(baseValue) * percentage / 100.0;
                return baseValue + (random.NextDouble() * 2 * deviation - deviation);
            }

            int macdFastPeriod = Math.Max(1, GetRandomIntWithinDeviation(baseThresholds.MacdFastPeriod, maxDeviationPercentage));

            int smaShortTerm = GetRandomIntWithinDeviation(baseThresholds.SmaShortTerm, maxDeviationPercentage);
            yield return new IndicatorThresholds
            {
                // RSI thresholds
                RsiOverbought = Math.Abs(GetRandomIntWithinDeviation(baseThresholds.RsiOverbought, maxDeviationPercentage)),
                RsiOversold = Math.Abs(GetRandomIntWithinDeviation(baseThresholds.RsiOversold, maxDeviationPercentage)),
                RsiPeriod = Math.Max(1, GetRandomIntWithinDeviation(baseThresholds.RsiPeriod, maxDeviationPercentage)),

                // MACD thresholds
                MacdFastPeriod = macdFastPeriod,
                MacdSlowPeriod = Math.Max(macdFastPeriod + 1, GetRandomIntWithinDeviation(baseThresholds.MacdSlowPeriod, maxDeviationPercentage)),
                MacdSignalPeriod = Math.Max(1, GetRandomIntWithinDeviation(baseThresholds.MacdSignalPeriod, maxDeviationPercentage)),
                MacdSignalLine = GetRandomDecimalWithinDeviation(baseThresholds.MacdSignalLine, maxDeviationPercentage),

                // ATR thresholds
                AtrMultiplier = GetRandomDecimalWithinDeviation(baseThresholds.AtrMultiplier, maxDeviationPercentage),
                AtrPeriod = Math.Max(2, GetRandomIntWithinDeviation(baseThresholds.AtrPeriod, maxDeviationPercentage)),

                // SMA thresholds
                SmaShortTerm = Math.Max(1, smaShortTerm),
                SmaLongTerm = Math.Max(smaShortTerm + 1, GetRandomIntWithinDeviation(baseThresholds.SmaLongTerm, maxDeviationPercentage)),

                // Parabolic SAR thresholds
                ParabolicSarStep = Math.Max(0.005d, GetRandomDoubleWithinDeviation(baseThresholds.ParabolicSarStep, maxDeviationPercentage)),
                ParabolicSarMax = Math.Max(GetRandomDoubleWithinDeviation(baseThresholds.ParabolicSarMax, maxDeviationPercentage), baseThresholds.ParabolicSarStep * 1.1),

                // Bollinger Bands thresholds
                BollingerBandsPeriod = Math.Max(2, GetRandomIntWithinDeviation(baseThresholds.BollingerBandsPeriod, maxDeviationPercentage)),
                BollingerBandsDeviation = GetRandomDoubleWithinDeviation(baseThresholds.BollingerBandsDeviation, maxDeviationPercentage),

                // ADX thresholds
                AdxStrongTrend = GetRandomDoubleWithinDeviation(baseThresholds.AdxStrongTrend, maxDeviationPercentage),
                AdxPeriod = Math.Max(2, GetRandomIntWithinDeviation(baseThresholds.AdxPeriod, maxDeviationPercentage)),

                // Stochastic thresholds
                StochasticOverbought = GetRandomDoubleWithinDeviation(baseThresholds.StochasticOverbought ?? 0, maxDeviationPercentage),
                StochasticOversold = GetRandomDoubleWithinDeviation(baseThresholds.StochasticOversold ?? 0, maxDeviationPercentage),
                StochasticPeriod = Math.Max(1, GetRandomIntWithinDeviation(baseThresholds.StochasticPeriod, maxDeviationPercentage)),
                StochasticSignalPeriod = Math.Max(1, GetRandomIntWithinDeviation(baseThresholds.StochasticSignalPeriod, maxDeviationPercentage)),

                // ROC thresholds
                RocPeriod = Math.Max(1, GetRandomIntWithinDeviation(baseThresholds.RocPeriod, maxDeviationPercentage)),

                // Buy/Sell thresholds
                BuyThreshold = Math.Abs(GetRandomIntWithinDeviation(baseThresholds.BuyThreshold, maxDeviationPercentage)),
                SellThreshold = Math.Abs(GetRandomIntWithinDeviation(baseThresholds.SellThreshold, maxDeviationPercentage)),

                ScoreMultiplier = GetRandomDoubleWithinDeviation(baseThresholds.ScoreMultiplier ?? 1, maxDeviationPercentage)
            };
        }
    }
}