using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitBetMatic
{
    public class AdvancedStrategy : TradingStrategyBase, ITradingStrategy
    {
        public AdvancedStrategy()
        {
            Thresholds = new IndicatorThresholds
            {
                RsiOverbought = 70,
                RsiOversold = 30,
                MacdSignalLine = 0m,
                AtrMultiplier = 1.5m,
                SmaLongTerm = 200,
                BollingerBandsPeriod = 20,
                BollingerBandsDeviation = 2.0d,
                AdxStrongTrend = 25,
                ParabolicSarStep = 0.02d,
                ParabolicSarMax = 0.2d
            };
        }

        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice)
        {

            // Bereken Indicatoren
            var atr = quotes.GetAtr(14).LastOrDefault();
            var ema200 = quotes.GetEma(Thresholds.SmaLongTerm).LastOrDefault(); // Gebruik de Threshold waarde
            var rsi = quotes.GetRsi(14).LastOrDefault();
            var macd = quotes.GetMacd().LastOrDefault();
            var bb = quotes.GetBollingerBands(Thresholds.BollingerBandsPeriod, Thresholds.BollingerBandsDeviation).LastOrDefault(); // Gebruik Thresholds voor Bollinger Bands
            var adx = quotes.GetAdx(14).LastOrDefault();
            var psar = quotes.GetParabolicSar(Thresholds.ParabolicSarStep, Thresholds.ParabolicSarMax).LastOrDefault(); // Gebruik Thresholds voor Parabolic SAR

            if (atr == null || ema200 == null || rsi == null || macd == null || bb == null || adx == null || psar == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var buyScore = 0;
            var sellScore = 0;

            // RSI Scoring
            if (rsi.Rsi < Thresholds.RsiOversold)
                buyScore += (int)((Thresholds.RsiOversold - rsi.Rsi) / Thresholds.RsiOversold * 100);
            else if (rsi.Rsi > Thresholds.RsiOverbought)
                sellScore += (int)((rsi.Rsi - Thresholds.RsiOverbought) / (100 - Thresholds.RsiOverbought) * 100);

            // MACD Scoring
            if (macd.Macd > macd.Signal)
                buyScore += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 100 * 100);
            else
                sellScore += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 100 * 100);

            // Bollinger Bands Scoring
            if (currentPrice < Functions.ToDecimal(bb.LowerBand))
                buyScore += (int)((Functions.ToDecimal(bb.LowerBand) - currentPrice) / Functions.ToDecimal(bb.LowerBand) * 100);
            else if (currentPrice > Functions.ToDecimal(bb.UpperBand))
                sellScore += (int)((currentPrice - Functions.ToDecimal(bb.UpperBand)) / Functions.ToDecimal(bb.UpperBand) * 100);

            // EMA200 Cross Scoring
            if (currentPrice > Functions.ToDecimal(ema200.Ema))
                buyScore += (int)((currentPrice - Functions.ToDecimal(ema200.Ema)) / Functions.ToDecimal(ema200.Ema) * 100);
            else if (currentPrice < Functions.ToDecimal(ema200.Ema))
                sellScore += (int)((Functions.ToDecimal(ema200.Ema) - currentPrice) / Functions.ToDecimal(ema200.Ema) * 100);

            // ADX Scoring
            if (adx.Adx > Thresholds.AdxStrongTrend)
                if (currentPrice > Functions.ToDecimal(ema200.Ema))
                    buyScore += (int)(adx.Adx / 50 * 100);
                else
                    sellScore += (int)(adx.Adx / 50 * 100);

            // Parabolic SAR Scoring
            if (currentPrice > Functions.ToDecimal(psar.Sar))
                buyScore += 50; // Geeft potentieel koop signaal
            else if (currentPrice < Functions.ToDecimal(psar.Sar))
                sellScore += 50; // Geeft potentieel verkoop signaal

            BuySellHold signal;
            var finalScore = 0;
            if (buyScore > sellScore)
            {
                signal = BuySellHold.Buy;
                finalScore = buyScore;
            }
            else if (sellScore > buyScore)
            {
                signal = BuySellHold.Sell;
                finalScore = sellScore;
            }
            else
            {
                signal = BuySellHold.Hold;
            }

            return (signal, finalScore);
        }

        public override string Interval() => "1h";

        public override int Limit() => Thresholds.SmaLongTerm; // Gebruik de Threshold waarde voor Limiet
    }
}
