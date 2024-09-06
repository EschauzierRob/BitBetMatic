using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitBetMatic
{
    public class StoplossStrategy : TradingStrategyBase, ITradingStrategy
    {
        public StoplossStrategy()
        {
            Thresholds = new IndicatorThresholds
            {
                RsiOverbought = 74,
                RsiOversold = 10,
                MacdSignalLine = 0.0m,
                AtrMultiplier = 1.5m,
                SmaShortTerm = 50,
                SmaLongTerm = 168,
                ParabolicSarStep = 0.02,
                ParabolicSarMax = 0.2,
                BollingerBandsPeriod = 20,
                BollingerBandsDeviation = 2.3889067556335224,
                AdxStrongTrend = 25.0,
                StochasticOverbought = 80.0,
                StochasticOversold = 20.0,
                BuyThreshold = 35,
                SellThreshold = -74,
                RsiPeriod = 140,
                AtrPeriod = 14,
                StochasticPeriod = 14,
                StochasticSignalPeriod = 3,
                MacdFastPeriod = 14,
                MacdSlowPeriod = 18,
                MacdSignalPeriod = 5,
                AdxPeriod = 14,
                RocPeriod = 15
            }
            ;
        }

        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice)
        {
            // Bereken indicatoren
            var atr = quotes.GetAtr(14).LastOrDefault();
            var ema200 = quotes.GetEma(Thresholds.SmaLongTerm).LastOrDefault();
            var rsi = quotes.GetRsi(14).LastOrDefault();
            var macd = quotes.GetMacd().LastOrDefault();
            var bb = quotes.GetBollingerBands(Thresholds.BollingerBandsPeriod, Thresholds.BollingerBandsDeviation).LastOrDefault();

            if (atr == null || ema200 == null || rsi == null || macd == null || bb == null || bb.UpperBand == null || bb.LowerBand == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var score = 0;
            var signal = BuySellHold.Hold;

            try
            {
                // RSI Scoring
                if (rsi.Rsi < Thresholds.RsiOversold)
                {
                    score += (int)((Thresholds.RsiOversold - rsi.Rsi) / Thresholds.RsiOversold * 100);
                }
                else if (rsi.Rsi > Thresholds.RsiOverbought)
                {
                    score += (int)((rsi.Rsi - Thresholds.RsiOverbought) / (100 - Thresholds.RsiOverbought) * 100);
                }

                // MACD Scoring
                score += (int)Math.Abs(Functions.ToDecimal(macd.Histogram) * Thresholds.MacdSignalLine);

                // Bollinger Bands Scoring
                if (currentPrice < Functions.ToDecimal(bb.LowerBand))
                {
                    score += (int)((Functions.ToDecimal(bb.LowerBand) - currentPrice) / Functions.ToDecimal(bb.LowerBand) * 100);
                }
                else if (currentPrice > Functions.ToDecimal(bb.UpperBand))
                {
                    score += (int)((currentPrice - Functions.ToDecimal(bb.UpperBand)) / Functions.ToDecimal(bb.UpperBand) * 100);
                }

                // EMA200 Cross Scoring
                if (currentPrice > Functions.ToDecimal(ema200.Ema))
                {
                    score += (int)((currentPrice - Functions.ToDecimal(ema200.Ema)) / Functions.ToDecimal(ema200.Ema) * 100);
                }
                else if (currentPrice < Functions.ToDecimal(ema200.Ema))
                {
                    score += (int)((Functions.ToDecimal(ema200.Ema) - currentPrice) / Functions.ToDecimal(ema200.Ema) * 100);
                }

                // Dynamic Stop Loss Calculation using ATR
                var atrValue = Functions.ToDecimal(atr.Atr);
                var stopLossThreshold = currentPrice - (Thresholds.AtrMultiplier * atrValue);

                if (currentPrice < stopLossThreshold)
                {
                    signal = BuySellHold.Sell;
                    score = 200; // Hoge score voor onmiddellijke actie
                }
                else if (score >= 50)
                {
                    signal = currentPrice > Functions.ToDecimal(ema200.Ema) ? BuySellHold.Buy : BuySellHold.Sell;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnalyzeMarketWithStopLoss: KABOOM: {e.Message}");
            }

            return (signal, score);
        }

        public override string Interval() => "15m";

        public override int Limit() => Thresholds.SmaLongTerm;
    }
}
