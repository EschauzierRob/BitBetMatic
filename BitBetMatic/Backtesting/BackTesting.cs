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

    public BackTesting(IApiWrapper api)
    {
        dataLoader = new DataLoader(api);
        indicatorThresholdPersistency = new IndicatorThresholdPersistency();
    }

    private (ITradingStrategy strategy, decimal result, string resultText) RunBacktest(ITradingStrategy strategy, string market, List<Quote> historicalData)
    {
        var portfolioManager = new PortfolioManager();
        portfolioManager.SetCash(300);
        var strategyExecutor = new StrategyExecutor(strategy);

        var tradeActions = strategyExecutor.ExecuteStrategy(market, historicalData, portfolioManager);

        foreach (var action in tradeActions)
        {
            portfolioManager.ExecuteTrade(action);
        }

        var resultAnalyzer = new ResultAnalyzer(strategy, tradeActions, portfolioManager);
        return (strategy, portfolioManager.GetAccountTotal(), resultAnalyzer.Analyze());
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
        sb.AppendLine($"{market} backtesting:");
        var strat = await GetMostPerformantStrategyVariant<TStrat>(sb, market, numberOfVariants);

        strat.Thresholds.Market = market;
        strat.Thresholds.Strategy = strat.GetType().Name;
        await indicatorThresholdPersistency.InsertThresholdsAsync(strat.Thresholds);

        string thresholds = JsonConvert.SerializeObject(((TStrat)strat).Thresholds);

        sb.AppendLine("Thresholds: ");
        sb.AppendLine(thresholds);

        string result = sb.ToString();
        Console.Write(result);

        return (strat, result);
    }

    private async Task<ITradingStrategy> GetMostPerformantStrategy(StringBuilder sb, string market)
    {
        List<ITradingStrategy> strategies = new List<ITradingStrategy>
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
            var testRes = RunBacktest(strat, market, historicalData);
            sb.AppendLine(testRes.resultText);
            if (res.total < testRes.result)
            {
                res = (testRes.strategy, testRes.result);
            }
        }
        return res.strategy;
    }
    private async Task<List<Quote>> GetHistoricalData(string market, string interval = "1h", DateTime? start = null, DateTime? end = null)
    {
        // Voorstel voor meerdere tijdspannes (60 dagen, 180 dagen, 365 dagen)
        start ??= DateTime.Today.AddDays(-60);
        end ??= DateTime.Today;
        var historicalData = await dataLoader.LoadHistoricalData(market, interval, 1440, start.Value, end.Value);
        return historicalData;
    }

    private async Task<TradingStrategyBase> GetMostPerformantStrategyVariant<TStrat>(StringBuilder sb, string market, int numberOfVariants) where TStrat : TradingStrategyBase, new()
    {
        TStrat strategy = new TStrat();
        var thresholds = await indicatorThresholdPersistency.GetLatestThresholdsAsync(strategy.GetType().Name, market) ?? strategy.Thresholds;
        var thresholdVariants = GenerateThresholdVariations(thresholds, numberOfVariants);
        List<TStrat> strategies = new List<TStrat> { strategy };

        foreach (var thresholdVariant in thresholdVariants)
        {
            strategies.Add(new TStrat { Thresholds = thresholdVariant });
        }

        (TradingStrategyBase strategy, decimal total) res = (strategies.First(), decimal.Zero);

        // Gebruik verschillende tijdspannes
        var historicalDataLong = await GetHistoricalData(market, res.strategy.Interval(), DateTime.Today.AddDays(-365));
        var historicalDataMedium = historicalDataLong.Where(x => x.Date > DateTime.Today.AddDays(-180)).ToList();
        var historicalDataShort = historicalDataMedium.Where(x => x.Date > DateTime.Today.AddDays(-30)).ToList();

        List<Task<(TStrat strategy, decimal result, string resultText)>> tasks = new List<Task<(TStrat strategy, decimal result, string resultText)>>();

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
                string combinedText = $"SHORT: {shortTermResult.resultText}\nMEDIUM: {mediumTermResult.resultText}\nLONG: {longTermResult.resultText}\nCombined Result: {combinedResult}";

                return (strategy: strat, result: combinedResult, resultText: combinedText);
            }));
        }

        // Wacht op alle taken om te voltooien
        var results = await Task.WhenAll(tasks);

        // Verwerk de resultaten
        foreach (var testRes in results)
        {
            sb.AppendLine(testRes.resultText);
            if (res.total < testRes.result)
            {
                Console.WriteLine($"new highest: {testRes.result}");
                res = (testRes.strategy, testRes.result);
            }
        }

        sb.AppendLine($"Winning variant of {nameof(res.strategy)} got a total result of {res.total}");
        return res.strategy;
    }

    private IEnumerable<IndicatorThresholds> GenerateThresholdVariations(IndicatorThresholds baseThresholds, int variationCount)
    {
        var random = new Random();
        for (int i = 0; i < variationCount; i++)
        {
            int macdFastPeriod = baseThresholds.MacdFastPeriod + random.Next(-3, 3);
            int macdSlowPeriod = baseThresholds.MacdSlowPeriod + random.Next(-6, 6);
            yield return new IndicatorThresholds
            {
                SmaLongTerm = baseThresholds.SmaLongTerm + random.Next(-15, 15),
                RsiPeriod = baseThresholds.RsiPeriod + random.Next(-3, 3),
                RsiOverbought = baseThresholds.RsiOverbought + random.Next(-8, 8),
                RsiOversold = baseThresholds.RsiOversold + random.Next(-8, 8),
                MacdFastPeriod = macdFastPeriod,
                MacdSlowPeriod = Math.Max(macdFastPeriod + 1, macdSlowPeriod),
                MacdSignalPeriod = baseThresholds.MacdSignalPeriod + random.Next(-2, 2),
                BollingerBandsPeriod = baseThresholds.BollingerBandsPeriod + random.Next(-5, 5),
                BollingerBandsDeviation = baseThresholds.BollingerBandsDeviation + (random.NextDouble() * 0.2d),
                RocPeriod = baseThresholds.RocPeriod + random.Next(-3, 3),
                BuyThreshold = baseThresholds.BuyThreshold + random.Next(-15, 15),
                SellThreshold = baseThresholds.SellThreshold + random.Next(-15, 15)
            };
        }
    }
}