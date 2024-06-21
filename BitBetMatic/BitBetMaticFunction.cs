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
using System.Text;

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
            var btcPrice = await GetPrice(client, "BTC-EUR");
            var ethPrice = await GetPrice(client, "ETH-EUR");

            var btcQuotes = await GetCandleData(client, "BTC-EUR");
            var ethQuotes = await GetCandleData(client, "ETH-EUR");

            var btcRsi = CalculateRsi(btcQuotes);
            var ethRsi = CalculateRsi(ethQuotes);

            string decisionBtc = MakeDecision(btcPrice, btcQuotes, "BTC");
            string decisionEth = MakeDecision(ethPrice, ethQuotes, "ETH");

            return new OkObjectResult($"{decisionBtc}\n{decisionEth}");
        }

        private static async Task<decimal> GetPrice(RestClient client, string market)
        {
            var request = new RestRequest($"ticker/price?market={market}", Method.Get);
            var response = await client.ExecuteAsync(request);
            var price = JsonConvert.DeserializeObject<dynamic>(response.Content).price;
            return (decimal)price;
        }

        private static async Task<List<Quote>> GetCandleData(RestClient client, string market)
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

        private static decimal CalculateRsi(List<Quote> quotes)
        {
            var rsiResults = quotes.GetRsi(14).ToList();
            var latestRsi = rsiResults.Last().Rsi;

            return latestRsi.HasValue ? Convert.ToDecimal(latestRsi.Value) : 0;
        }

        private static decimal CalculateMeanReversion(List<Quote> quotes)
        {
            var smaResults = quotes.GetSma(20).ToList();
            var latestSma = smaResults.Last().Sma;

            return latestSma.HasValue ? Convert.ToDecimal(latestSma.Value) : 0;
        }

        private static decimal CalculateMomentum(List<Quote> quotes)
        {
            var macdResults = quotes.GetMacd(12, 26, 9).ToList();
            var latestMacd = macdResults.Last();

            return latestMacd.Macd.HasValue ? Convert.ToDecimal(latestMacd.Macd.Value) : 0;
        }

        private static (decimal Macd, decimal Signal, decimal Histogram) CalculateMacd(List<Quote> quotes)
        {
            var macd = quotes.GetMacd(12, 26, 9);
            var lastMacd = macd.Last();
            return (ToDecimal(lastMacd.Macd), ToDecimal(lastMacd.Signal), ToDecimal(lastMacd.Histogram));
        }

        private static (decimal UpperBand, decimal LowerBand) CalculateBollingerBands(List<Quote> quotes)
        {
            var bollinger = quotes.GetBollingerBands(20, 2);
            var lastBollinger = bollinger.Last();
            return (ToDecimal(lastBollinger.UpperBand), ToDecimal(lastBollinger.LowerBand));
        }

        private static decimal ToDecimal(double? doubleVal){
            return doubleVal.HasValue ? Convert.ToDecimal(doubleVal.Value) : 0;
        }

        private static string MakeDecision(decimal tokenPrice, List<Quote> quotes, string symbol)
        {
            var rsi = CalculateRsi(quotes);

            var meanReversion = CalculateMeanReversion(quotes);

            var momentum = CalculateMomentum(quotes);

            var macd = CalculateMacd(quotes);

            var bollinger = CalculateBollingerBands(quotes);

            string Decision = MakeSingleDecision(quotes, tokenPrice, rsi, meanReversion, momentum, symbol, macd, bollinger);

            return $"{Decision}";
        }

        private static string MakeSingleDecision(
            List<Quote> quotes, decimal price, decimal rsi, decimal meanReversion, decimal momentum, string symbol, (decimal Macd, decimal Signal, decimal Histogram) macd, (decimal UpperBand, decimal LowerBand) bollinger)
        {
            StringBuilder buyArguments = new StringBuilder();
            StringBuilder sellArguments = new StringBuilder();
            string decision;

            // Decision logic for Token
            if (rsi < 30 && momentum > 0)
            {
                buyArguments.AppendLine($"- (Oversold with positive momentum).");
            }
            else if (rsi > 70 && momentum < 0)
            {
                sellArguments.AppendLine($"- (Overbought with negative momentum). ");
            }
            
            if (price < meanReversion && momentum > 0)
            {
                buyArguments.AppendLine($"- (Below mean with positive momentum). ");
            }
            else if (price > meanReversion && momentum < 0)
            {
                sellArguments.AppendLine($"- (Above mean with negative momentum). ");
            }
            
            if (macd.Macd > macd.Signal)
            {
                buyArguments.AppendLine($"- (MACD positive). ");
            }
            else if (macd.Macd < macd.Signal)
            {
                sellArguments.AppendLine($"- (MACD negative). ");
            }
            
            if (price < bollinger.LowerBand)
            {
                buyArguments.AppendLine($"- (Below Bollinger Lower Band). ");
            }
            else if (price > bollinger.UpperBand)
            {
                sellArguments.AppendLine($"- (Above Bollinger Upper Band). ");
            }

            if(buyArguments.Length>sellArguments.Length) {
                decision =$"Buy {symbol}\n"+
                buyArguments;
            }
            else if(buyArguments.Length<sellArguments.Length) {
                decision =$"Sell {symbol}\n"+
                sellArguments;
            }
            else {
                decision =$"Hold {symbol}. ";
            }

            return $"Decision: {decision}\n\n" +
                                      $"BTC Price: {quotes.Last().Close}\n" +
                                      $"BTC RSI: {rsi}\n" +
                                      $"BTC Mean Reversion: {meanReversion}\n" +
                                      $"BTC Momentum: {momentum}\n" +
                                      $"BTC MACD: {macd.Macd}, Signal: {macd.Signal}, Histogram: {macd.Histogram}\n" +
                                      $"BTC Bollinger Bands - Upper: {bollinger.UpperBand}, Lower: {bollinger.LowerBand}\n";
        }
    }
}
