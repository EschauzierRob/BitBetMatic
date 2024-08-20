using BitBetMatic.API;
using Newtonsoft.Json;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class BitBetMaticProcessor
    {
        public BitBetMaticProcessor()
        {
            api = new BitvavoApi();
            dataLoader = new DataLoader(api);
        }

        public const string BtcMarket = "BTC-EUR";
        public const string EthMarket = "ETH-EUR";
        private BitvavoApi api;
        private DataLoader dataLoader;
        private PortfolioManager PortfolioManager;
        public async Task<string> RunStrategies(ITradingStrategy btcStrategy, ITradingStrategy ethStrategy, bool transact = true)
        {
            PortfolioManager = new PortfolioManager();

            var sb = new StringBuilder();
            sb.AppendLine($"\nTrading advice:\n");

            // var markets = GetMarkets(false);
            await EnactStrategy(transact, sb, new List<string> { BtcMarket }, btcStrategy);
            await EnactStrategy(transact, sb, new List<string> { EthMarket }, ethStrategy);

            string result = sb.ToString();
            Console.Write(result);

            return result;
        }

        private async Task EnactStrategy(bool transact, StringBuilder sb, List<string> markets, ITradingStrategy strategy)
        {
            var analyses = new Dictionary<string, (BuySellHold Signal, int Score)>();
            sb.AppendLine($"\nEnacting strategy '{strategy.GetType().Name}':\n");

            var balances = await api.GetBalances();
            var euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");

            foreach (var market in markets)
            {
                var quotes = await api.GetCandleData(market, strategy.Interval(), strategy.Limit());
                var currentPrice = await api.GetPrice(market);
                var analysis = strategy.AnalyzeMarket(market, quotes, currentPrice);
                analyses.Add(market, analysis);
            }

            // Sort analyses: Hold first, then Sell, then Buy
            var orderedAnalyses = analyses
                .OrderBy(x => x.Value.Signal == BuySellHold.Hold ? 0 : (x.Value.Signal == BuySellHold.Sell ? 1 : 2)).Select(x => (market: x.Key, analysis: x.Value))
                .ToList();

            // First execute all sell orders
            foreach (var analysis in orderedAnalyses)
            {
                var tokenBalance = balances.FirstOrDefault(x => x.symbol == Functions.GetSymbolFormMarket(analysis.market));
                PortfolioManager.SetCash(euroBalance.available);
                var price = await api.GetPrice(analysis.market);
                PortfolioManager.SetTokenBalance(analysis.market, tokenBalance.available, price);
                var outcome = strategy.CalculateOutcome(price, analysis.analysis.Score, analysis.analysis.Signal, PortfolioManager, analysis.market);
                sb.AppendLine($" - {outcome.action}, at a score of {analysis.analysis.Score}");

                if (transact && outcome.amount > 0)
                {
                    sb.AppendLine($"TRANSACTING: {analysis.analysis.Signal} order for {outcome.amount:F} in {analysis.market} market");
                    await ProcessOrdering((analysis.analysis.Signal, outcome.amount, analysis.market));
                }

                if (analysis.analysis.Signal == BuySellHold.Buy || analysis.analysis.Signal == BuySellHold.Sell)
                {
                    // Update balances after each order
                    balances = await api.GetBalances();
                    euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");
                }
            }
        }

        private async Task ProcessOrdering((BuySellHold signal, decimal amount, string market) outcome)
        {
            switch (outcome.signal)
            {
                case BuySellHold.Buy:
                    await api.Buy(outcome.market, outcome.amount);
                    break;
                case BuySellHold.Sell:
                    await api.Sell(outcome.market, outcome.amount);
                    break;
                default: break;
            }
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
            // string thresholds = JsonConvert.SerializeObject(((StoplossStrategy)strategyBtc).Thresholds);

            // sb.AppendLine("Thresholds: ");
            // sb.AppendLine(thresholds);

            string result = sb.ToString();
            Console.Write(result);


            return (strategyBtc, strategyEth, result);
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

        private async Task<ITradingStrategy> GetMostPerformantStrategyVariant<TStrat>(StringBuilder sb, string market) where TStrat : TradingStrategyBase, new()
        {
            var thresholdVariants = GenerateThresholdVariations(new TStrat().Thresholds, 20);
            List<TStrat> strategies = new List<TStrat> { new TStrat() };

            foreach (var thresholdVariant in thresholdVariants)
            {
                strategies.Add(new TStrat { Thresholds = thresholdVariant });
            }

            (ITradingStrategy strategy, decimal total) res = (strategies.First(), decimal.Zero);

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

        private List<string> GetMarkets(bool top10)
        {
            var markets = new List<string> { BtcMarket, EthMarket };
            if (top10)
            {
                var marketsResult = api.GetMarkets().Result.OrderByDescending(x => x.Quote.Volume).Take(10);
                markets = marketsResult.Select(x => $"{x.Base.Symbol}-EUR").ToList();
            }

            return markets;
        }

        private static string FormatBalances(List<Balance> balances)
        {
            var balanceString = new StringBuilder();
            balanceString.AppendLine($"Balances available:");

            foreach (var balance in balances)
            {
                balanceString.AppendLine($"{balance.available} {balance.symbol}");
            }

            return balanceString.ToString();
        }
    }
}