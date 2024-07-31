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
        private const string BtcMarket = "BTC-EUR";
        private const string EthMarket = "ETH-EUR";
        private BitvavoApi api;
        public async Task<IActionResult> Process(bool transact = true)
        {
            api = new BitvavoApi();

            var sb = new StringBuilder();
            // sb.AppendLine(FormatBalances(balances));
            sb.AppendLine($"\nTrading advice:\n");

            var markets = GetMarkets(false);
            await EnactStrategy(transact, sb, markets, Strategy.ModerateStrategy);
            await EnactStrategy(transact, sb, markets, Strategy.AgressiveStrategy);
            await EnactStrategy(transact, sb, markets, Strategy.ScoredStrategy);
            await EnactStrategy(transact, sb, markets, Strategy.StoplossStrategy);
            await EnactStrategy(transact, sb, markets, Strategy.AdvancedStrategy);

            return new OkObjectResult(sb.ToString());
        }

        private async Task EnactStrategy(bool transact, StringBuilder sb, List<string> markets, Strategy strategy)
        {
            var tradingStrategy = new TradingStrategy(api, strategy);
            var analyses = new Dictionary<string, (BuySellHold Signal, int Score)>();
            sb.AppendLine($"\nEnacting strategy '{Enum.GetName(typeof(Strategy), tradingStrategy.GetStrategy())}':\n");

            var balances = await api.GetBalances();
            var euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");

            foreach (var market in markets)
            {
                var analysis = await tradingStrategy.AnalyzeMarket(market);
                analyses.Add(market, analysis);
            }

            // Sort analyses: Hold first, then Sell, then Buy
            var orderedAnalyses = analyses
                .OrderBy(x => x.Value.Signal == BuySellHold.Hold ? 0 : (x.Value.Signal == BuySellHold.Sell ? 1 : 2)).Select(x => (market: x.Key, analysis: x.Value))
                .ToList();

            // First execute all sell orders
            foreach (var analysis in orderedAnalyses)
            {
                var tokenBalance = balances.FirstOrDefault(x => x.symbol == GetSymbolFormMarket(analysis.market));
                var outcome = await TransactOutcome(api, analysis.analysis.Score, analysis.analysis.Signal, euroBalance, tokenBalance, analysis.market, transact);

                if (analysis.analysis.Signal == BuySellHold.Buy || analysis.analysis.Signal == BuySellHold.Sell)
                {
                    // Update balances after each order
                    balances = await api.GetBalances();
                    euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");
                }

                sb.AppendLine($"{outcome}");
            }
        }

        private static async Task<string> TransactOutcome(IApiWrapper api, int score, BuySellHold outcome, Balance euroBalance, Balance tokenBalance, string market, bool transact)
        {
            string action;
            var price = await api.GetPrice(market);
            var percentagePerScore = Functions.ToDecimal(score / 1000d);

            var tokenBalanced = (tokenBalance?.available ?? 0) * price;
            var euroBalanced = euroBalance?.available ?? 0;

            var euroPercentageAmount = euroBalanced * percentagePerScore;
            var tokenPercentageAmount = tokenBalanced * percentagePerScore;
            var minOrderAmount = Functions.ToDecimal(5);

            switch (outcome)
            {
                case BuySellHold.Buy:
                    {
                        if (minOrderAmount > euroBalanced)
                        {
                            action = $"Holding: Can't buy {euroBalanced:F2} of {market}, because euro balance is lower than minimum ordersize";
                            break;
                        }
                        var amount = Functions.GetHigher(euroPercentageAmount, minOrderAmount);
                        action = $"Buying {amount:F2} euro worth of {market}";
                        if (transact)
                        {
                            await api.Buy(market, amount);
                        }
                    }
                    break;
                case BuySellHold.Sell:
                    {
                        if (minOrderAmount > tokenBalanced)
                        {
                            action = $"Holding: Can't sell {tokenBalanced:F2} of {market}, because token balance is lower than minimum ordersize";
                            break;
                        }
                        var amount = Functions.GetHigher(tokenPercentageAmount, minOrderAmount);
                        action = $"Selling {amount:F2} euro worth of {market}";
                        if (transact)
                        {
                            await api.Sell(market, amount);
                        }
                    }
                    break;
                default:
                    action = $"Holding {tokenBalanced:F2} of {market}";
                    break;
            }

            Console.WriteLine(action);

            return $"{action}, at a score of {score}";
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

        private string GetSymbolFormMarket(string market)
        {
            return market.Substring(0, 3);
        }
    }
}