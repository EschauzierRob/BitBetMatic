using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitBetMatic
{
    public class ModerateStrategy : TradingStrategyBase
    {
        public ModerateStrategy()
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
        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice)
        {
            // Bereken Indicatoren
            var ema200 = quotes.GetEma(Thresholds.SmaLongTerm).LastOrDefault(); // Gebruik Thresholds waarde voor lange termijn EMA
            var rsi = quotes.GetRsi(Thresholds.RsiPeriod).LastOrDefault(); // Gebruik Thresholds waarde voor RSI
            var macd = quotes.GetMacd(Thresholds.MacdFastPeriod, Thresholds.MacdSlowPeriod, Thresholds.MacdSignalPeriod).LastOrDefault(); // Gebruik Thresholds voor MACD
            var bb = quotes.GetBollingerBands(Thresholds.BollingerBandsPeriod, Thresholds.BollingerBandsDeviation).LastOrDefault(); // Gebruik Thresholds voor Bollinger Bands
            var roc = quotes.GetRoc(Thresholds.RocPeriod).LastOrDefault(); // Gebruik Thresholds waarde voor ROC (Momentum)

            if (ema200 == null || rsi == null || macd == null || bb == null || roc == null || bb.UpperBand == null || bb.LowerBand == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var score = 0;
            var signal = BuySellHold.Hold;

            // RSI Scoring
            if (rsi.Rsi < Thresholds.RsiOversold)
                score += (int)((Thresholds.RsiOversold - rsi.Rsi) / Thresholds.RsiOversold * 100);
            else if (rsi.Rsi > Thresholds.RsiOverbought)
                score += (int)((rsi.Rsi - Thresholds.RsiOverbought) / (100 - Thresholds.RsiOverbought) * 100);

            // MACD Scoring
            score += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 100 * 100); // Pas deze factor aan op basis van typische MACD-waarden

            // Bollinger Bands Scoring
            if (currentPrice < Functions.ToDecimal(bb.LowerBand))
                score += (int)((Functions.ToDecimal(bb.LowerBand) - currentPrice) / Functions.ToDecimal(bb.LowerBand) * 100);
            else if (currentPrice > Functions.ToDecimal(bb.UpperBand))
                score += (int)((currentPrice - Functions.ToDecimal(bb.UpperBand)) / Functions.ToDecimal(bb.UpperBand) * 100);

            // EMA200 Cross Scoring
            if (currentPrice > Functions.ToDecimal(ema200.Ema))
                score += (int)((currentPrice - Functions.ToDecimal(ema200.Ema)) / Functions.ToDecimal(ema200.Ema) * 100);
            else if (currentPrice < Functions.ToDecimal(ema200.Ema))
                score += (int)((Functions.ToDecimal(ema200.Ema) - currentPrice) / Functions.ToDecimal(ema200.Ema) * 100);

            // ROC (Momentum) Scoring
            score += (int)(Math.Abs(Functions.ToDecimal(roc.Roc)) / 100 * 100); // Pas deze factor aan op basis van typische ROC-waarden

            // Bepaal Signaal
            if (score >= Thresholds.BuyThreshold)
                signal = currentPrice > Functions.ToDecimal(ema200.Ema) ? BuySellHold.Buy : BuySellHold.Sell;

            return (signal, score);
        }

        public override string Interval() => "15m";

        public override int Limit() => Thresholds.SmaLongTerm; // Gebruik de Thresholds waarde voor limiet
    }
}
