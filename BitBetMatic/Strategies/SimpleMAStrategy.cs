
using System.Collections.Generic;
using System.Linq;
using BitBetMatic.API;
using Skender.Stock.Indicators;

namespace BitBetMatic
{
    public class SimpleMAStrategy : TradingStrategyBase
    {
        public SimpleMAStrategy()
        {
            Thresholds = new IndicatorThresholds
            {
                RsiOverbought = 69,
                RsiOversold = 25,
                MacdSignalLine = 0.0m,
                AtrMultiplier = 1.5m,
                SmaShortTerm = 50,
                SmaLongTerm = 169,
                ParabolicSarStep = 0.02,
                ParabolicSarMax = 0.2,
                BollingerBandsPeriod = 13,
                BollingerBandsDeviation = 2.362817089383719,
                AdxStrongTrend = 25.0,
                StochasticOverbought = 80.0,
                StochasticOversold = 20.0,
                BuyThreshold = 44,
                SellThreshold = -54,
                RsiPeriod = 16,
                AtrPeriod = 14,
                StochasticPeriod = 14,
                StochasticSignalPeriod = 3,
                MacdFastPeriod = 6,
                MacdSlowPeriod = 34,
                MacdSignalPeriod = 10,
                AdxPeriod = 14,
                RocPeriod = 12

            };
        }
        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<FlaggedQuote> quotes, decimal currentPrice)
        {
            // Bereken Indicatoren
            var ema20 = quotes.GetEma(20).LastOrDefault(); // Gebruik Thresholds waarde voor lange termijn EMA
            var ema50 = quotes.GetEma(50).LastOrDefault(); // Gebruik Thresholds waarde voor lange termijn EMA

            var buySignal = ema20.Ema > ema50.Ema;
            return buySignal ? (BuySellHold.Buy, 30000) : (BuySellHold.Sell, 30000);
        }

        public override string Interval() => "15m";

        public override int Limit() => Thresholds.SmaLongTerm; // Gebruik de Thresholds waarde voor limiet
    }
}