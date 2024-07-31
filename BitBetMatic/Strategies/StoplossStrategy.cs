using BitBetMatic.API;
using Skender.Stock.Indicators;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class StoplossStrategy : TradingStrategyBase, ITradingStrategy
    {
        public StoplossStrategy(BitvavoApi api) : base(api) { }

        public override async Task<(BuySellHold Signal, int Score)> AnalyzeMarket(string market)
        {
            {
                var quotes = await Api.GetCandleData(market, "1h", "200");
                var currentPrice = await Api.GetPrice(market);

                // Calculate Indicators
                var atr = quotes.GetAtr(14).LastOrDefault();
                var ema200 = quotes.GetEma(200).LastOrDefault();
                var rsi = quotes.GetRsi(14).LastOrDefault();
                var macd = quotes.GetMacd().LastOrDefault();
                var bb = quotes.GetBollingerBands().LastOrDefault();

                if (atr == null || ema200 == null || rsi == null || macd == null || bb == null)
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
                        score += (int)((rsi.Rsi - 70) / 30 * 100);

                    // MACD Scoring
                    score += (int)(Math.Abs(Functions.ToDecimal(macd.Histogram)) / 100 * 100);

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

                    // Dynamic Stop Loss Calculation
                    var atrValue = Functions.ToDecimal(atr.Atr);
                    var stopLossThreshold = currentPrice - (1.5m * atrValue);

                    if (currentPrice < stopLossThreshold)
                    {
                        signal = BuySellHold.Sell;
                        score = 200; // High score for immediate action
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
        }
    }
}
