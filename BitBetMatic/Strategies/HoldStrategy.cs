using BitBetMatic.API;
using Skender.Stock.Indicators;
using System.Collections.Generic;

namespace BitBetMatic
{
    public class HoldStrategy : TradingStrategyBase, ITradingStrategy
    {

        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<FlaggedQuote> quotes, decimal currentPrice)
        {

            return (BuySellHold.Hold, 0);
        }

        public override string Interval() => "1h";

        public override int Limit() => 200;
    }
}
