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

        public async Task<IActionResult> Process(bool transact = true)
        {
            var api = new BitvavoApi();
            var balances = await api.GetBalances();

            var sb = new StringBuilder();
            sb.AppendLine(FormatBalances(balances));
            sb.AppendLine($"\nModerate advice:\n");
            sb.AppendLine($"{ProcessModerate(api, balances, BtcMarket, "BTC", transact).Result}");
            sb.AppendLine($"{ProcessModerate(api, balances, EthMarket, "ETH", transact).Result}");
            sb.AppendLine($"\nAgressive advice:\n");
            sb.AppendLine($"{ProcessAgressive(api, balances, BtcMarket, "BTC", false).Result}");
            sb.AppendLine($"{ProcessAgressive(api, balances, EthMarket, "ETH", transact).Result}");

            return new OkObjectResult(sb.ToString());
        }
        public async Task<string> ProcessModerate(BitvavoApi api, List<Balance> balances, string market, string symbol, bool transact = true)
        {
            var tradingStrategy = new TradingStrategy(api);
            var analyse = tradingStrategy.AnalyzeMarketModerate(market).Result;

            var euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");
            var tokenBalance = balances.FirstOrDefault(x => x.symbol == symbol);

            var outcome = await TransactOutcome(api, analyse.Score, analyse.Signal, euroBalance, tokenBalance, market, transact);

            return $" - {outcome}";
        }
        public async Task<string> ProcessAgressive(BitvavoApi api, List<Balance> balances, string market, string symbol, bool transact = true)
        {
            var tradingStrategy = new TradingStrategy(api);
            var analyse = tradingStrategy.AnalyzeMarketAgressive(market).Result;

            var euroBalance = balances.FirstOrDefault(x => x.symbol == "EUR");
            var tokenBalance = balances.FirstOrDefault(x => x.symbol == symbol);

            var outcome = await TransactOutcome(api, analyse.Score, analyse.Signal, euroBalance, tokenBalance, market, transact);

            return $" - {outcome}";
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

        private static async Task<string> TransactOutcome(BitvavoApi api, int score, BuySellHold outcome, Balance euroBalance, Balance tokenBalance, string market, bool transact)
        {
            string action;
            var price = await api.GetPrice(market);
            var percentagePerScore = Functions.ToDecimal(score / 1000d);

            var tokenBalanced = (tokenBalance?.available ?? 0) * price;
            var euroBalanced = euroBalance?.available ?? 0;

            var euroPercentageAmount = euroBalanced * percentagePerScore;
            var tokenPercentageAmount = tokenBalanced * percentagePerScore;
            var minOrderAmount = Functions.ToDecimal(5);

            if (outcome == BuySellHold.Buy)
            {
                var amount = Functions.GetHigher(euroPercentageAmount, minOrderAmount);
                action = $"Buying {amount} euro worth of {market}";
                if (transact)
                {
                    await api.Buy(market, amount);
                }
            }
            else if (outcome == BuySellHold.Sell)
            {
                var amount = Functions.GetHigher(tokenPercentageAmount, minOrderAmount);
                action = $"Selling {amount} euro worth of {market}";
                if (transact)
                {
                    await api.Sell(market, amount);
                }
            }
            else
            {
                action = $"Holding {tokenBalanced} of {market}";
            }
            Console.WriteLine(action);

            return action + $", at a score of {score}";
        }
    }
}