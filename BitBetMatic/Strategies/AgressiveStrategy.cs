using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitBetMatic
{
    public class AgressiveStrategy : TradingStrategyBase, ITradingStrategy
    {
        public AgressiveStrategy()
        {
            Thresholds = new IndicatorThresholds
            {
                RsiOverbought = 74,
                RsiOversold = 12,
                MacdSignalLine = 0.0M,
                AtrMultiplier = 1.5M,
                SmaShortTerm = 50,
                SmaLongTerm = 172,
                ParabolicSarStep = 0.02,
                ParabolicSarMax = 0.2,
                BollingerBandsPeriod = 10,
                BollingerBandsDeviation = 2.981814445647155,
                AdxStrongTrend = 25.0,
                StochasticOverbought = 80.0,
                StochasticOversold = 20.0,
                BuyThreshold = 76,
                SellThreshold = -17,
                RsiPeriod = 12,
                AtrPeriod = 14,
                StochasticPeriod = 14,
                StochasticSignalPeriod = 3,
                MacdFastPeriod = 17,
                MacdSlowPeriod = 18,
                MacdSignalPeriod = 4,
                AdxPeriod = 14,
                RocPeriod = 21
            }
            ;
        }
        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice)
        {
            // Bereken Indicatoren
            var ema50 = quotes.GetEma(Thresholds.SmaShortTerm).LastOrDefault(); // Gebruik de Thresholds waarde voor korte termijn EMA
            var ema200 = quotes.GetEma(Thresholds.SmaLongTerm).LastOrDefault(); // Gebruik de Thresholds waarde voor lange termijn EMA
            var rsi = quotes.GetRsi(Thresholds.RsiPeriod).LastOrDefault(); // Gebruik de Thresholds waarde voor RSI
            var macd = quotes.GetMacd(Thresholds.MacdFastPeriod, Thresholds.MacdSlowPeriod, Thresholds.MacdSignalPeriod).LastOrDefault(); // Gebruik Thresholds voor MACD
            var bb = quotes.GetBollingerBands(Thresholds.BollingerBandsPeriod, Thresholds.BollingerBandsDeviation).LastOrDefault(); // Gebruik Thresholds voor Bollinger Bands
            var stochastic = quotes.GetStoch(Thresholds.StochasticPeriod, Thresholds.StochasticSignalPeriod).LastOrDefault(); // Gebruik Thresholds voor Stochastic Oscillator
            var adx = quotes.GetAdx(Thresholds.AdxPeriod).LastOrDefault(); // Gebruik Thresholds voor ADX

            if (ema50 == null || ema200 == null || rsi == null || macd == null || bb == null || stochastic == null || adx == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var score = 0;
            var signal = BuySellHold.Hold;

            // Aangepaste RSI Scoring
            if (rsi.Rsi < Thresholds.RsiOversold)
                score += (int)((Thresholds.RsiOversold - rsi.Rsi) / Thresholds.RsiOversold * 100);
            else if (rsi.Rsi > Thresholds.RsiOverbought)
                score += (int)((rsi.Rsi - Thresholds.RsiOverbought) / (100 - Thresholds.RsiOverbought) * 100);

            // MACD Scoring
            score += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 50 * 100); // Verhoogde gevoeligheid voor MACD

            // Bollinger Bands Scoring
            if (currentPrice < Functions.ToDecimal(bb.LowerBand))
                score += (int)((Functions.ToDecimal(bb.LowerBand) - currentPrice) / Functions.ToDecimal(bb.LowerBand) * 100);
            else if (currentPrice > Functions.ToDecimal(bb.UpperBand))
                score += (int)((currentPrice - Functions.ToDecimal(bb.UpperBand)) / Functions.ToDecimal(bb.UpperBand) * 100);

            // EMA Cross Scoring
            if (currentPrice > Functions.ToDecimal(ema50.Ema))
                score += (int)((currentPrice - Functions.ToDecimal(ema50.Ema)) / Functions.ToDecimal(ema50.Ema) * 100);
            if (currentPrice > Functions.ToDecimal(ema200.Ema))
                score += (int)((currentPrice - Functions.ToDecimal(ema200.Ema)) / Functions.ToDecimal(ema200.Ema) * 100);
            else if (currentPrice < Functions.ToDecimal(ema200.Ema))
                score += (int)((Functions.ToDecimal(ema200.Ema) - currentPrice) / Functions.ToDecimal(ema200.Ema) * 100);

            // Stochastic Oscillator Scoring
            if (stochastic.K < Thresholds.StochasticOversold)
                score += (int)((Thresholds.StochasticOversold - stochastic.K) / Thresholds.StochasticOversold * 100);
            else if (stochastic.K > Thresholds.StochasticOverbought)
                score += (int)((stochastic.K - Thresholds.StochasticOverbought) / (100 - Thresholds.StochasticOverbought) * 100);

            // ADX Scoring
            if (adx.Adx > Thresholds.AdxStrongTrend)
                score += (int)((adx.Adx - Thresholds.AdxStrongTrend) / (100 - Thresholds.AdxStrongTrend) * 100);

            // Bepaal Signaal
            if (score >= Thresholds.BuyThreshold)
                signal = currentPrice > Functions.ToDecimal(ema200.Ema) ? BuySellHold.Buy : BuySellHold.Sell;

            return (signal, score);
        }

        public override string Interval() => "1h";

        public override int Limit() => Thresholds.SmaLongTerm; // Gebruik de Thresholds waarde voor limiet
    }
}
