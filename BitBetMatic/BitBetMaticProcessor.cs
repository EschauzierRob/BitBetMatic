using BitBetMatic.API;
using BitBetMatic.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class BitBetMaticProcessor
    {
        public BitBetMaticProcessor(IApiWrapper api, ICandleRepository candleRepository)
        {
            _api = api;
            _candleRepository = candleRepository;
        }

        public const string BtcMarket = "BTC-EUR";
        public const string EthMarket = "ETH-EUR";
        private IApiWrapper _api;
        private ICandleRepository _candleRepository;
        private PortfolioManager PortfolioManager;
        public async Task<string> RunStrategies(ITradingStrategy btcStrategy, ITradingStrategy ethStrategy, bool transact = true)
        {
            PortfolioManager = new PortfolioManager();

            var sb = new StringBuilder();
            sb.AppendLine($"\nTrading advice:\n");

            // var markets = GetMarkets(false);
            var balances = await _api.GetBalances();
            await EnactStrategy(balances, transact, sb, new List<string> { BtcMarket }, btcStrategy);
            await EnactStrategy(balances, transact, sb, new List<string> { EthMarket }, ethStrategy);

            string result = sb.ToString();
            Console.Write(result);

            return result;
        }

        private async Task EnactStrategy(List<Balance> balances, bool transact, StringBuilder sb, List<string> markets, ITradingStrategy strategy)
        {
            var analyses = new Dictionary<string, (BuySellHold Signal, int Score)>();
            sb.AppendLine($"\nEnacting strategy '{strategy.GetType().Name}':\n");

            var euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");

            foreach (var market in markets)
            {
                var quotes = await _candleRepository.GetCandlesAsync(market, DateTime.Now.AddDays(-15), DateTime.Now);

                if (quotes.Count == 0)
                {
                    quotes = await _api.GetCandleData(market, strategy.Interval(), strategy.Limit(), DateTime.Now.AddDays(-15), DateTime.Now);
                    await _candleRepository.AddCandlesAsync(quotes);
                }

                var currentPrice = await _api.GetPrice(market);
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
                var tokenBalance = balances.FirstOrDefault(x => x.symbol == Functions.GetSymbolFromMarket(analysis.market));
                PortfolioManager.SetCash(euroBalance.available);
                var price = await _api.GetPrice(analysis.market);
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
                    balances = await _api.GetBalances();
                    euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");
                }
            }
        }

        private async Task ProcessOrdering((BuySellHold signal, decimal amount, string market) outcome)
        {
            switch (outcome.signal)
            {
                case BuySellHold.Buy:
                    await _api.Buy(outcome.market, outcome.amount);
                    break;
                case BuySellHold.Sell:
                    await _api.Sell(outcome.market, outcome.amount);
                    break;
                default: break;
            }
        }

        private List<string> GetMarkets(bool top10)
        {
            var markets = new List<string> { BtcMarket, EthMarket };
            if (top10)
            {
                var marketsResult = _api.GetMarkets().Result.OrderByDescending(x => x.Quote.Volume).Take(10);
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

        public async Task<(ITradingStrategy strategyBtc, ITradingStrategy strategyEth, string result)> RunBacktesting(StringBuilder sb)
        {
            BackTesting backTesting = new BackTesting(_api, _candleRepository);
            return await backTesting.RunBacktesting(sb);
        }
    }
}