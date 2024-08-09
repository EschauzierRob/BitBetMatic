using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitBetMatic;
using Skender.Stock.Indicators;

public class StrategyExecutor
{
    private readonly ITradingStrategy _strategy;

    public StrategyExecutor(ITradingStrategy strategy)
    {
        _strategy = strategy;
    }

    public List<TradeAction> ExecuteStrategy(string market, List<Quote> data, PortfolioManager portfolioManager)
    {
        var actions = new List<TradeAction>();
        var size = _strategy.Limit();
        data = data.OrderBy(x => x.Date).ToList();

        for (var i = size; i <= data.Count; i++)
        {
            var candle = data.Skip(i - size).Take(size).ToList();
            decimal currentPrice = candle.Last().Close;
            var analysis = _strategy.AnalyzeMarket(market, candle, currentPrice);
            var (amount, _) = _strategy.CalculateOutcome(currentPrice, analysis.Score, analysis.Signal, portfolioManager, market);

            var tradeAction = new TradeAction
            {
                Market = market,
                AmountInEuro = amount,
                Action = analysis.Signal,
                CurrentTokenPrice = currentPrice,
                Timestamp = candle.Last().Date
            };
            actions.Add(tradeAction);

            //Special case: if we use the HoldStrategy, we only want to buy 100% into the token at the start, and then just hold forever :D
            if (_strategy.GetType() == typeof(HoldStrategy) && portfolioManager.GetCashBalance() == 300)
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
