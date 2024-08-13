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
                SmaLongTerm = 200,
                RsiPeriod = 14,
                RsiOverbought = 70,
                RsiOversold = 30,
                MacdFastPeriod = 12,
                MacdSlowPeriod = 26,
                MacdSignalPeriod = 9,
                BollingerBandsPeriod = 20,
                BollingerBandsDeviation = 2.0d,
                RocPeriod = 14,
                BuyThreshold = 50,
                SellThreshold = -50
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

            if (ema200 == null || rsi == null || macd == null || bb == null || roc == null)
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

        public override string Interval() => "1h";

        public override int Limit() => Thresholds.SmaLongTerm; // Gebruik de Thresholds waarde voor limiet
    }
}
