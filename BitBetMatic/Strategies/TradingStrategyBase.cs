using Skender.Stock.Indicators;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public abstract class TradingStrategyBase : ITradingStrategy
    {
        public abstract string Interval();
        public abstract int Limit();

        public TradingStrategyBase()
        {
        }

        public abstract (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice);
        public (decimal amount, string action) CalculateOutcome(decimal currentPrice, int score, BuySellHold outcome, PortfolioManager portfolioManager, string market)
        {
            string action;
            var percentagePerScore = Functions.ToDecimal(score / 1000d);

            var euroBalance = portfolioManager.GetCashBalance();
            var tokenBalance = portfolioManager.GetAssetTokenBalance(market);
            var tokenBalanced = tokenBalance * currentPrice;
            var euroBalanced = euroBalance;

            var euroPercentageAmount = euroBalanced * percentagePerScore;
            var tokenPercentageAmount = tokenBalanced * percentagePerScore;
            var minOrderAmount = Functions.ToDecimal(5);
            var amount = Functions.ToDecimal(0);

            switch (outcome)
            {
                case BuySellHold.Buy:
                    {
                        if (minOrderAmount > euroBalanced)
                        {
                            action = $"Holding: Can't buy {euroBalanced:F2} of {market}, because euro balance is lower than minimum ordersize";
                            break;
                        }
                        amount = Functions.GetHigher(euroPercentageAmount, minOrderAmount);
                        action = $"Buying {amount:F2} euro worth of {market}";
                    }
                    break;
                case BuySellHold.Sell:
                    {
                        if (minOrderAmount > tokenBalanced)
                        {
                            action = $"Holding: Can't sell {tokenBalanced:F2} of {market}, because token balance is lower than minimum ordersize";
                            break;
                        }
                        amount = Functions.GetHigher(tokenPercentageAmount, minOrderAmount);
                        action = $"Selling {amount:F2} euro worth of {market}";
                    }
                    break;
                default:
                    action = $"Holding {tokenBalanced:F2} of {market}";
                    break;
            }

            // Console.WriteLine($"tokenBalanced:{tokenBalanced:F2}, euroBalance:{euroBalance:F2}, currentPrice:{currentPrice:F2}, action:{action}");

            return (amount, action);
        }

    }
}
