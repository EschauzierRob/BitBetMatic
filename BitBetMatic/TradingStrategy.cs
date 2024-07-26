using Skender.Stock.Indicators;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class TradingStrategy
    {
        private readonly BitvavoApi Api;

        public TradingStrategy(BitvavoApi api)
        {
            Api = api;
        }
        public async Task<(BuySellHold Signal, int Score)> AnalyzeMarketModerate(string market)
        {
            var quotes = await Api.GetCandleData(market, "1h", "200");
            var currentPrice = await Api.GetPrice(market);

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


        public async Task<(BuySellHold Signal, int Score)> AnalyzeMarketAgressive(string market)
        {
            var quotes = await Api.GetCandleData(market, "15m", "200"); // Shorter interval
            var currentPrice = await Api.GetPrice(market);

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




        public async Task<(BuySellHold Signal, int Score)> AnalyzeMarket(string market)
        {
            var quotes = await Api.GetCandleData(market, "15m", "200");
            var currentPrice = await Api.GetPrice(market);

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

            return (signal, Math.Abs(score));
        }
    }
}