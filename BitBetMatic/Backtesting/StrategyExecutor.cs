using System.Collections.Generic;
using System.Linq;
using BitBetMatic;
using BitBetMatic.API;

public class StrategyExecutor
{
    private readonly ITradingStrategy Strategy;

    public StrategyExecutor(ITradingStrategy strategy)
    {
        Strategy = strategy;
    }

    public List<TradeAction> ExecuteStrategy(string market, List<FlaggedQuote> data, PortfolioManager portfolioManager)
    {
        var actions = new List<TradeAction>();
        var size = Strategy.Limit();
        data = data.OrderBy(x => x.Date).ToList();

        var buys = 0;
        var sells = 0;
        var holds = 0;

        for (var i = size; i <= data.Count; i++)
        {
            var candle = data.Skip(i - size).Take(size).ToList();
            decimal currentPrice = candle.Last().Close;
            var analysis = Strategy.AnalyzeMarket(market, candle, currentPrice);
            var (amount, _) = Strategy.CalculateOutcome(currentPrice, analysis.Score, analysis.Signal, portfolioManager, market);

            var tradeAction = new TradeAction
            {
                Market = market,
                AmountInEuro = amount,
                Action = analysis.Signal,
                CurrentTokenPrice = currentPrice,
                Timestamp = candle.Last().Date
            };
            actions.Add(tradeAction);

            if (analysis.Signal == BuySellHold.Buy) buys++;
            else if (analysis.Signal == BuySellHold.Sell) sells++;
            else if (analysis.Signal == BuySellHold.Hold) holds++;

            //Special case: if we use the HoldStrategy, we only want to buy 100% into the token at the start, and then just hold forever :D
            if (Strategy.GetType() == typeof(HoldStrategy) && portfolioManager.GetCashBalance() == 300)
            {
                tradeAction = new TradeAction
                {
                    Market = market,
                    AmountInEuro = 300,
                    Action = BuySellHold.Buy,
                    CurrentTokenPrice = currentPrice,
                    Timestamp = candle.Last().Date
                };
            }

            // Execute the trade and update the portfolio state
            portfolioManager.ExecuteTrade(tradeAction);
        }
        portfolioManager.SetTokenCurrentPrice(market, data.Last().Close);

        return actions;
    }
}
