using Microsoft.AspNetCore.Mvc;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class BitBetMaticProcessor
    {
        private const string BtcMarket = "BTC-EUR";
        private const string EthMarket = "ETH-EUR";

        public async Task<IActionResult> Process()
        {
            var api = new BitvavoApi();

            // Run in parallel to improve performance
            var btcTask = ProcessForToken(api, BtcMarket, "BTC");
            var ethTask = ProcessForToken(api, EthMarket, "ETH");
            await Task.WhenAll(btcTask, ethTask);

            var btcDecision = btcTask.Result;
            var ethDecision = ethTask.Result;

            var balance = await api.GetBalance();
            var euroBalance = balance.FirstOrDefault(x => x.symbol == "EUR");
            var btcBalance = balance.FirstOrDefault(x => x.symbol == "BTC");
            var ethBalance = balance.FirstOrDefault(x => x.symbol == "ETH");

            await TransactOutcome(api, btcDecision.decisions.GetOutcome(), euroBalance, btcBalance, BtcMarket);
            await TransactOutcome(api, ethDecision.decisions.GetOutcome(), euroBalance, ethBalance, EthMarket);

            return new OkObjectResult($"Balance: {balance}\n\n Decisions:\n{btcDecision.text}\n{ethDecision.text}");
        }

        private static async Task TransactOutcome(BitvavoApi api, BuySellHold outcome, Balance euroBalance, Balance tokenBalance, string market)
        {
            var price = await api.GetPrice(market);

            if (outcome == BuySellHold.Buy && euroBalance?.available > 0)
            {
                decimal amount = euroBalance.available/10;
                await api.Buy(market, amount);
            }
            else if (outcome == BuySellHold.Sell && tokenBalance?.available > 0)
            {
                decimal amount = tokenBalance.available*price/10;
                await api.Sell(market, amount);
            }
        }

        private async Task<(string text, Decisions decisions)> ProcessForToken(BitvavoApi api, string pair, string symbol)
        {
            var price = await api.GetPrice(pair);
            var quotes = await api.GetCandleData(pair);

            var indicators = CalculateIndicators(quotes);
            var decision = MakeDecision(price, indicators);
            var text = DecisionToText(symbol, price, indicators, decision);

            return (text, decision);
        }

        private string DecisionToText(string symbol, decimal tokenPrice, Indicators indicators, Decisions decisions)
        {
            var outcome = decisions.GetOutcome();
            var outcomeText = outcome switch
            {
                BuySellHold.Buy => "Buy",
                BuySellHold.Sell => "Sell",
                _ => "Hold"
            };

            var arguments = string.Join(Environment.NewLine, decisions.GetOutcomeArguments().Select(x => $" - {x.Text}"));
            var decision = $"{outcomeText} {symbol}\n{arguments}";

            return $"{decision}\n" +
                   $"Price: {tokenPrice}\n" +
                   $"RSI: {indicators.Rsi}\n" +
                   $"Mean Reversion: {indicators.MeanReversion}\n" +
                   $"Momentum: {indicators.Momentum}\n" +
                   $"MACD: {indicators.Macd.MacdValue}, Signal: {indicators.Macd.Signal}, Histogram: {indicators.Macd.Histogram}\n" +
                   $"Bollinger Bands - Upper: {indicators.Bollinger.UpperBand}, Lower: {indicators.Bollinger.LowerBand}\n";
        }

        private decimal CalculateRsi(List<Quote> quotes)
        {
            var rsiResults = quotes.GetRsi(14).ToList();
            return ToDecimal(rsiResults.LastOrDefault()?.Rsi);
        }

        private decimal CalculateMeanReversion(List<Quote> quotes)
        {
            var smaResults = quotes.GetSma(20).ToList();
            return ToDecimal(smaResults.LastOrDefault()?.Sma);
        }

        private decimal CalculateMomentum(List<Quote> quotes)
        {
            var momResults = quotes.GetMacd(10).ToList();
            return ToDecimal(momResults.LastOrDefault()?.Macd);
        }

        private Macd CalculateMacd(List<Quote> quotes)
        {
            var macdResults = quotes.GetMacd(12, 26, 9).ToList();
            var latestMacd = macdResults.Last();
            return new Macd(ToDecimal(latestMacd?.Macd), ToDecimal(latestMacd?.Signal), ToDecimal(latestMacd?.Histogram));
        }

        private BollingerBand CalculateBollingerBands(List<Quote> quotes)
        {
            var bollingerResults = quotes.GetBollingerBands(20, 2).ToList();
            var latestBollinger = bollingerResults.Last();
            return new BollingerBand(ToDecimal(latestBollinger?.UpperBand), ToDecimal(latestBollinger?.LowerBand));
        }

        private decimal ToDecimal(double? doubleVal) => doubleVal.HasValue ? Convert.ToDecimal(doubleVal.Value) : 0;

        private Decisions MakeDecision(decimal tokenPrice, Indicators indicators)
        {
            var decisions = new Decisions();

            if (indicators.Rsi < 30 && indicators.Momentum > 0)
            {
                decisions[Decisions.RsiAndMomentum].Outcome = BuySellHold.Buy;
                decisions[Decisions.RsiAndMomentum].Text = "RSI < 30 and positive momentum";
            }
            else if (indicators.Rsi > 70 && indicators.Momentum < 0)
            {
                decisions[Decisions.RsiAndMomentum].Outcome = BuySellHold.Sell;
                decisions[Decisions.RsiAndMomentum].Text = "RSI > 70 and negative momentum";
            }
            else
            {
                decisions[Decisions.RsiAndMomentum].Outcome = BuySellHold.Hold;
                decisions[Decisions.RsiAndMomentum].Text = $"RSI({indicators.Rsi}) and momentum({indicators.Momentum})";
            }

            if (tokenPrice < indicators.MeanReversion && indicators.Momentum > 0)
            {
                decisions[Decisions.MeanReversionAndMomentum].Outcome = BuySellHold.Buy;
                decisions[Decisions.MeanReversionAndMomentum].Text = "Below mean reversion with positive momentum";
            }
            else if (tokenPrice > indicators.MeanReversion && indicators.Momentum < 0)
            {
                decisions[Decisions.MeanReversionAndMomentum].Outcome = BuySellHold.Sell;
                decisions[Decisions.MeanReversionAndMomentum].Text = "Above mean reversion with negative momentum";
            }
            else
            {
                decisions[Decisions.MeanReversionAndMomentum].Outcome = BuySellHold.Hold;
                decisions[Decisions.MeanReversionAndMomentum].Text = $"MeanReversion({indicators.MeanReversion}) and momentum({indicators.Momentum})";
            }

            if (indicators.Macd.MacdValue > indicators.Macd.Signal)
            {
                decisions[Decisions.Macd].Outcome = BuySellHold.Buy;
                decisions[Decisions.Macd].Text = "MACD > Signal";
            }
            else if (indicators.Macd.MacdValue < indicators.Macd.Signal)
            {
                decisions[Decisions.Macd].Outcome = BuySellHold.Sell;
                decisions[Decisions.Macd].Text = "MACD < Signal";
            }
            else
            {
                decisions[Decisions.Macd].Outcome = BuySellHold.Hold;
                decisions[Decisions.Macd].Text = $"MacdValue({indicators.Macd.MacdValue}) and Signal({indicators.Macd.Signal})";
            }

            if (tokenPrice < indicators.Bollinger.LowerBand)
            {
                decisions[Decisions.Bollinger].Outcome = BuySellHold.Buy;
                decisions[Decisions.Bollinger].Text = "Below Bollinger Lower Band";
            }
            else if (tokenPrice > indicators.Bollinger.UpperBand)
            {
                decisions[Decisions.Bollinger].Outcome = BuySellHold.Sell;
                decisions[Decisions.Bollinger].Text = "Above Bollinger Upper Band";
            }
            else
            {
                decisions[Decisions.Bollinger].Outcome = BuySellHold.Hold;
                decisions[Decisions.Bollinger].Text = $"tokenPrice({tokenPrice}) and Bollinger.LowerBand({indicators.Bollinger.LowerBand}) ";
            }

            return decisions;
        }

        private Indicators CalculateIndicators(List<Quote> quotes)
        {
            return new Indicators(CalculateRsi(quotes), CalculateMeanReversion(quotes), CalculateMomentum(quotes), CalculateMacd(quotes), CalculateBollingerBands(quotes));
        }
    }
}