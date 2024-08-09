using BitBetMatic.API;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class ScoredStrategy : TradingStrategyBase, ITradingStrategy
    {
        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice)
        {
            // Calculate Indicators
            var ema200 = quotes.GetEma(200).LastOrDefault();
            var rsi = quotes.GetRsi(14).LastOrDefault();
            var macd = quotes.GetMacd().LastOrDefault();
            var bb = quotes.GetBollingerBands().LastOrDefault();
            var stochastic = quotes.GetStoch(14, 3).LastOrDefault();
            var atr = quotes.GetAtr(14).LastOrDefault();

            if (ema200 == null || rsi == null || macd == null || bb == null || stochastic == null || atr == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var score = 0;
            var signal = BuySellHold.Hold;
            try
            {
                // RSI Scoring
                if (rsi.Rsi < 32)
                    score += (int)((32 - rsi.Rsi) / 32 * 100);
                else if (rsi.Rsi > 70)
                    score -= (int)((rsi.Rsi - 70) / 30 * 100);

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
                if (stochastic.K > 80)
                    score -= (int)((stochastic.K - 80) / 20 * 100);
                else if (stochastic.K < 20)
                    score += (int)((20 - stochastic.K) / 20 * 100);

                // Determine Signal
                if (score >= 50)
                    signal = BuySellHold.Buy;
                else if (score <= -50)
                    signal = BuySellHold.Sell;
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnalyzeMarket: KABOOM: {e.Message}");
            }

            return (signal, Math.Abs(score));
        }
        public override string Interval() => "1h";

        public override int Limit() => 200;
    }
}
