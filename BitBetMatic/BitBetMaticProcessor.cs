using BitBetMatic.API;
using Microsoft.AspNetCore.Mvc;
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
        private PortfolioManager portfolioManager;
        public async Task<string> Process(bool transact = true)
        {
            api = new BitvavoApi();
            portfolioManager = new PortfolioManager();

            var sb = new StringBuilder();
            sb.AppendLine($"\nTrading advice:\n");

            var markets = GetMarkets(false);
            // await EnactStrategy(transact, sb, markets, new ModerateStrategy());
            // await EnactStrategy(transact, sb, markets, new AgressiveStrategy());
            // await EnactStrategy(transact, sb, markets, new ScoredStrategy());
            await EnactStrategy(transact, sb, new List<string>{BtcMarket}, new StoplossStrategy());
            await EnactStrategy(transact, sb, new List<string>{EthMarket}, new AdvancedStrategy());

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
                portfolioManager.SetCash(euroBalance.available);
                var price = await api.GetPrice(analysis.market);
                portfolioManager.SetTokenBalance(analysis.market, tokenBalance.available, price);
                var outcome = strategy.CalculateOutcome(price, analysis.analysis.Score, analysis.analysis.Signal, portfolioManager, analysis.market);
                sb.AppendLine($" - {outcome.action}, at a score of {analysis.analysis.Score}");
                if (transact)
                {
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

        private async Task ProcessOrdering((BuySellHold signal, decimal price, string market) outcome)
        {
            switch (outcome.signal)
            {
                case BuySellHold.Buy:
                    await api.Buy(outcome.market, outcome.price);
                    break;
                case BuySellHold.Sell:
                    await api.Sell(outcome.market, outcome.price);
                    break;
                default: break;
            }
        }

        public async Task<string> RunBacktest(ITradingStrategy strategy, string market)
        {
            portfolioManager = new PortfolioManager();
            portfolioManager.SetCash(300);
            var strategyExecutor = new StrategyExecutor(strategy);

            var historicalData = await dataLoader.LoadHistoricalData(market, "1h", 1440, DateTime.Today.AddMonths(-1), DateTime.Today);
            var tradeActions = strategyExecutor.ExecuteStrategy(market, historicalData, portfolioManager);

            foreach (var action in tradeActions)
            {
                portfolioManager.ExecuteTrade(action);
            }

            var resultAnalyzer = new ResultAnalyzer(strategy, tradeActions, portfolioManager);
            return resultAnalyzer.Analyze();
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