using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public static class BitBetMaticFunction
    {
        [FunctionName("BitBetMaticFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var client = new RestClient("https://api.bitvavo.com/v2");

            // Run in parallel to improve performance
            var btcTask = ProcessForToken(client, "BTC-EUR", "BTC");
            var ethTask = ProcessForToken(client, "ETH-EUR", "ETH");
            await Task.WhenAll(btcTask, ethTask);

            var decisionBtc = btcTask.Result;
            var decisionEth = ethTask.Result;

            return new OkObjectResult($"{decisionBtc.text}\n{decisionEth.text}");
        }

        private static async Task<(string text, Decisions decisions)> ProcessForToken(RestClient client, string pair, string symbol)
        {
            var price = await GetPrice(client, pair);
            var quotes = await GetCandleData(client, pair);

            var indicators = CalculateIndicators(quotes);
            var decision = MakeDecision(price, indicators);
            var text = DecisionToText(symbol, price, indicators, decision);

            return (text, decision);
        }

        private static string DecisionToText(string symbol, decimal tokenPrice, Indicators indicators, Decisions decisions)
        {
            var outcome = decisions.Outcome().First().Outcome;
            var outcomeText = outcome switch
            {
                BuySellHold.Buy => "Buy",
                BuySellHold.Sell => "Sell",
                _ => "Hold"
            };

            var arguments = string.Join(Environment.NewLine, decisions.Outcome().Select(x => $" - {x.Text}"));
            var decision = $"{outcomeText} {symbol}\n{arguments}";

            return $"{decision}\n" +
                   $"Price: {tokenPrice}\n" +
                   $"RSI: {indicators.Rsi}\n" +
                   $"Mean Reversion: {indicators.MeanReversion}\n" +
                   $"Momentum: {indicators.Momentum}\n" +
                   $"MACD: {indicators.Macd.MacdValue}, Signal: {indicators.Macd.Signal}, Histogram: {indicators.Macd.Histogram}\n" +
                   $"Bollinger Bands - Upper: {indicators.Bollinger.UpperBand}, Lower: {indicators.Bollinger.LowerBand}\n";
        }

        private static async Task<decimal> GetPrice(RestClient client, string market)
        {
            try
            {
                var request = new RestRequest($"ticker/price?market={market}", Method.Get);
                var response = await client.ExecuteAsync(request);
                var price = JsonConvert.DeserializeObject<dynamic>(response.Content).price;
                return (decimal)price;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting price for {market}: {ex.Message}");
            }
        }

        private static async Task<List<Quote>> GetCandleData(RestClient client, string market)
        {
            try
            {
                var request = new RestRequest($"{market}/candles", Method.Get);
                request.AddParameter("interval", "1h");
                request.AddParameter("limit", "100");
                var response = await client.ExecuteAsync(request);
                var candles = JsonConvert.DeserializeObject<dynamic>(response.Content);
                
            List<Quote> quotes = new List<Quote>();

            foreach (var candle in candles)
            {
                quotes.Add(new Quote
                {
                    Date = DateTimeOffset.FromUnixTimeMilliseconds((long)candle[0]).UtcDateTime,
                    Open = (decimal)candle[1],
                    High = (decimal)candle[2],
                    Low = (decimal)candle[3],
                    Close = (decimal)candle[4],
                    Volume = (decimal)candle[5]
                });
            }

            return quotes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting candle data for {market}: {ex.Message}");
            }
        }

        private static decimal CalculateRsi(List<Quote> quotes)
        {
            var rsiResults = quotes.GetRsi(14).ToList();
            return ToDecimal(rsiResults.LastOrDefault()?.Rsi);
        }

        private static decimal CalculateMeanReversion(List<Quote> quotes)
        {
            var smaResults = quotes.GetSma(20).ToList();
            return ToDecimal(smaResults.LastOrDefault()?.Sma);
        }

        private static decimal CalculateMomentum(List<Quote> quotes)
        {
            var momResults = quotes.GetMacd(10).ToList();
            return ToDecimal(momResults.LastOrDefault()?.Macd);
        }

        private static Macd CalculateMacd(List<Quote> quotes)
        {
            var macdResults = quotes.GetMacd(12, 26, 9).ToList();
            var latestMacd = macdResults.Last();
            return new Macd
            {
                MacdValue = ToDecimal(latestMacd?.Macd),
                Signal = ToDecimal(latestMacd?.Signal),
                Histogram = ToDecimal(latestMacd?.Histogram)
            };
        }

        private static BollingerBand CalculateBollingerBands(List<Quote> quotes)
        {
            var bollingerResults = quotes.GetBollingerBands(20, 2).ToList();
            var latestBollinger = bollingerResults.Last();
            return new BollingerBand
            {
                UpperBand = ToDecimal(latestBollinger?.UpperBand),
                LowerBand = ToDecimal(latestBollinger?.LowerBand)
            };
        }

        private static decimal ToDecimal(double? doubleVal) => doubleVal.HasValue ? Convert.ToDecimal(doubleVal.Value) : 0;

        private static Decisions MakeDecision(decimal tokenPrice, Indicators indicators)
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

        private static Indicators CalculateIndicators(List<Quote> quotes)
        {
            return new Indicators
            {
                Rsi = CalculateRsi(quotes),
                MeanReversion = CalculateMeanReversion(quotes),
                Momentum = CalculateMomentum(quotes),
                Macd = CalculateMacd(quotes),
                Bollinger = CalculateBollingerBands(quotes)
            };
        }

    }

    public class BollingerBand
    {
        public decimal UpperBand { get; set; }
        public decimal LowerBand { get; set; }
    }

    public class Macd
    {
        public decimal MacdValue { get; set; }
        public decimal Signal { get; set; }
        public decimal Histogram { get; set; }
    }

    public class Indicators
    {
        public decimal Rsi { get; set; }
        public decimal MeanReversion { get; set; }
        public decimal Momentum { get; set; }
        public Macd Macd { get; set; }
        public BollingerBand Bollinger { get; set; }
    }

    public class Decisions : Dictionary<string, Decision>
    {
        public const string RsiAndMomentum = "RsiAndMomentum";
        public const string MeanReversionAndMomentum = "MeanReversionAndMomentum";
        public const string Macd = "Macd";
        public const string Bollinger = "Bollinger";
        public Decisions()
        {
            Add(RsiAndMomentum, new Decision());
            Add(MeanReversionAndMomentum, new Decision());
            Add(Macd, new Decision());
            Add(Bollinger, new Decision());
        }

        public List<Decision> Outcome()
        {
            int buysCount = Buys().Count;
            int sellsCount = Sells().Count;
            int holdsCount = Holds().Count;

            if (buysCount > sellsCount && buysCount > holdsCount) { return Buys(); }
            else if (sellsCount > buysCount && sellsCount > holdsCount) { return Sells(); }
            return Holds();
        }

        public List<Decision> Buys()
        {
            return this.Where(x => x.Value.Outcome == BuySellHold.Buy).Select(x => x.Value).ToList();
        }

        public List<Decision> Sells()
        {
            return this.Where(x => x.Value.Outcome == BuySellHold.Sell).Select(x => x.Value).ToList();
        }

        public List<Decision> Holds()
        {
            return this.Where(x => x.Value.Outcome == BuySellHold.Hold).Select(x => x.Value).ToList();
        }
    }

    public class Decision
    {
        public Decision()
        {
            Outcome = BuySellHold.Inconclusive;
            Text = "";
        }
        public BuySellHold Outcome { get; set; }
        public string Text { get; set; }
    }

    public enum BuySellHold
    {
        Inconclusive = 0,
        Buy = 1,
        Sell = 2,
        Hold = 3,
    }
}