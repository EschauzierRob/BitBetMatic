using Newtonsoft.Json;
using RestSharp;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class BitvavoApi : IApiWrapper
    {

        private readonly RestClient Client;
        public BitvavoApi()
        {
            Client = new RestClient("https://api.bitvavo.com/v2");
        }
        public async Task<decimal> GetPrice(string market)
        {
            try
            {
                var request = new RestRequest($"ticker/price?market={market}", Method.Get);
                var response = await Client.ExecuteAsync(request);
                var price = JsonConvert.DeserializeObject<dynamic>(response.Content).price;
                return (decimal)price;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting price for {market}: {ex.Message}");
            }
        }

        public async Task<List<Quote>> GetCandleData(string market)
        {
            try
            {
                var request = new RestRequest($"{market}/candles", Method.Get);
                request.AddParameter("interval", "1h");
                request.AddParameter("limit", "100");
                var response = await Client.ExecuteAsync(request);
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
    }
}