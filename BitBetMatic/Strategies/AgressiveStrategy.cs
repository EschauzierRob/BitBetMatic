using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitBetMatic
{
    public class AgressiveStrategy : TradingStrategyBase, ITradingStrategy
    {
        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice)
        {
            // Calculate Indicators
            var ema50 = quotes.GetEma(50).LastOrDefault();
            var ema200 = quotes.GetEma(200).LastOrDefault();
            var rsi = quotes.GetRsi(14).LastOrDefault();
            var macd = quotes.GetMacd().LastOrDefault();
            var bb = quotes.GetBollingerBands().LastOrDefault();
            var stochastic = quotes.GetStoch(14, 3).LastOrDefault();
            var adx = quotes.GetAdx(14).LastOrDefault();

            if (ema50 == null || ema200 == null || rsi == null || macd == null || bb == null || stochastic == null || adx == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var score = 0;
            var signal = BuySellHold.Hold;

            // Adjusted RSI Scoring
            if (rsi.Rsi < 32)
                score += (int)((32 - rsi.Rsi) / 32 * 100);
            else if (rsi.Rsi > 70)
                score += (int)((rsi.Rsi - 70) / 30 * 100);

            // MACD Scoring
            score += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 50 * 100); // Increased sensitivity

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
            if (stochastic.K < 20)
                score += (int)((20 - stochastic.K) / 20 * 100);
            else if (stochastic.K > 80)
                score += (int)((stochastic.K - 80) / 20 * 100);

            // ADX Scoring
            if (adx.Adx > 25)
                score += (int)((adx.Adx - 25) / 75 * 100);

            // Determine Signal
            if (score >= 50)
                signal = currentPrice > Functions.ToDecimal(ema200.Ema) ? BuySellHold.Buy : BuySellHold.Sell;

            return (signal, score);
        }

        public override string Interval() => "15m";

        public override int Limit() => 200;
    }
}