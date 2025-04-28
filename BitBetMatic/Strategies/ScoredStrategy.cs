using BitBetMatic.API;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitBetMatic
{
    public class ScoredStrategy : TradingStrategyBase, ITradingStrategy
    {
        public ScoredStrategy()
        {
            Thresholds = new IndicatorThresholds
            {
                RsiOverbought = 74,
                RsiOversold = 21,
                MacdSignalLine = 0.0m,
                AtrMultiplier = 1.5m,
                SmaShortTerm = 50,
                SmaLongTerm = 149,
                ParabolicSarStep = 0.02,
                ParabolicSarMax = 0.2,
                BollingerBandsPeriod = 16,
                BollingerBandsDeviation = 2.5925600633477703,
                AdxStrongTrend = 25.0,
                StochasticOverbought = 80.0,
                StochasticOversold = 20.0,
                BuyThreshold = 40,
                SellThreshold = -43,
                RsiPeriod = 14,
                AtrPeriod = 14,
                StochasticPeriod = 14,
                StochasticSignalPeriod = 3,
                MacdFastPeriod = 3,
                MacdSlowPeriod = 35,
                MacdSignalPeriod = 9,
                AdxPeriod = 14,
                RocPeriod = 9
            }
            ;
        }
        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<FlaggedQuote> quotes, decimal currentPrice)
        {
            // Bereken Indicatoren
            var ema200 = quotes.GetEma(Thresholds.SmaLongTerm).LastOrDefault(); // Gebruik de Thresholds waarde
            var rsi = quotes.GetRsi(Thresholds.RsiPeriod).LastOrDefault(); // Gebruik de Thresholds waarde
            var macd = quotes.GetMacd().LastOrDefault();
            var bb = quotes.GetBollingerBands(Thresholds.BollingerBandsPeriod, Thresholds.BollingerBandsDeviation).LastOrDefault(); // Gebruik Thresholds voor Bollinger Bands
            var stochastic = quotes.GetStoch(Thresholds.StochasticPeriod, Thresholds.StochasticSignalPeriod).LastOrDefault(); // Gebruik Thresholds voor Stochastic Oscillator

            var atr = quotes.GetAtr(Thresholds.AtrPeriod).LastOrDefault(); // Gebruik Thresholds voor ATR

            if (ema200 == null || rsi == null || macd == null || bb == null || stochastic == null || atr == null || bb.UpperBand == null || bb.LowerBand == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var score = 0;
            var signal = BuySellHold.Hold;

            try
            {
                // RSI Scoring
                if (rsi.Rsi < Thresholds.RsiOversold)
                    score += (int)((Thresholds.RsiOversold - rsi.Rsi) / Thresholds.RsiOversold * 100);
                else if (rsi.Rsi > Thresholds.RsiOverbought)
                    score -= (int)((rsi.Rsi - Thresholds.RsiOverbought) / (100 - Thresholds.RsiOverbought) * 100);

                // MACD Scoring
                score += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 100 * 100);

                // Bollinger Bands Scoring
                if (currentPrice < Functions.ToDecimal(bb.LowerBand))
                    score += (int)((Functions.ToDecimal(bb.LowerBand) - currentPrice) / Functions.ToDecimal(bb.LowerBand) * 100);
                else if (currentPrice > Functions.ToDecimal(bb.UpperBand))
                    score -= (int)((currentPrice - Functions.ToDecimal(bb.UpperBand)) / Functions.ToDecimal(bb.UpperBand) * 100);

                // EMA200 Cross Scoring
                if (currentPrice > Functions.ToDecimal(ema200.Ema))
                    score += (int)((currentPrice - Functions.ToDecimal(ema200.Ema)) / Functions.ToDecimal(ema200.Ema) * 100);
                else if (currentPrice < Functions.ToDecimal(ema200.Ema))
                    score -= (int)((Functions.ToDecimal(ema200.Ema) - currentPrice) / Functions.ToDecimal(ema200.Ema) * 100);

                // Stochastic Oscillator Scoring
                if (stochastic.K > Thresholds.StochasticOverbought)
                    score -= (int)((stochastic.K - Thresholds.StochasticOverbought) / (100 - Thresholds.StochasticOverbought) * 100);
                else if (stochastic.K < Thresholds.StochasticOversold)
                    score += (int)((Thresholds.StochasticOversold - stochastic.K) / Thresholds.StochasticOversold * 100);

                // Signaal Bepaling
                if (score >= Thresholds.BuyThreshold)
                    signal = BuySellHold.Buy;
                else if (score <= Thresholds.SellThreshold)
                    signal = BuySellHold.Sell;
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnalyzeMarket: KABOOM: {e.Message}");
            }

            return (signal, Math.Abs(score));
        }

        public override string Interval() => "15m";

        public override int Limit() => Thresholds.SmaLongTerm; // Gebruik de Thresholds waarde voor Limiet
    }
}
