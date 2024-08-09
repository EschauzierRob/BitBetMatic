using BitBetMatic.API;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class ModerateStrategy : TradingStrategyBase
    {
        public override (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice)
        {
            // Calculate Indicators
            var ema200 = quotes.GetEma(200).LastOrDefault();
            var rsi = quotes.GetRsi(14).LastOrDefault();
            var macd = quotes.GetMacd().LastOrDefault();
            var bb = quotes.GetBollingerBands().LastOrDefault();
            var roc = quotes.GetRoc(14).LastOrDefault(); // Calculate momentum

            if (ema200 == null || rsi == null || macd == null || bb == null || roc == null)
            {
                return (BuySellHold.Inconclusive, 0);
            }

            var score = 0;
            var signal = BuySellHold.Hold;

            // RSI Scoring
            if (rsi.Rsi < 30)
                score += (int)((30 - rsi.Rsi) / 30 * 100);
            else if (rsi.Rsi > 70)
                score += (int)((rsi.Rsi - 70) / 30 * 100);

            // MACD Scoring
            score += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 100 * 100); // Adjust this factor based on typical MACD values for your data

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
            score += (int)(Math.Abs(Functions.ToDecimal(roc.Roc)) / 100 * 100); // Adjust this factor based on typical ROC values for your data

            // Determine Signal
            if (score >= 50)
                signal = currentPrice > Functions.ToDecimal(ema200.Ema) ? BuySellHold.Buy : BuySellHold.Sell;

            return (signal, score);
        }
        public override string Interval() => "1h";

        public override int Limit() => 200;
    }
}
