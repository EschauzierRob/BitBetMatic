using Newtonsoft.Json;
using RestSharp;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public class BitvavoApi : IApiWrapper
    {
        private readonly RestClient Client;

        public BitvavoApi()
        {
            Client = new RestClient(Environment.GetEnvironmentVariable("API_BASE_URL"));
        }

        private void SetApiRequestHeaders(RestRequest request, string url, Method method, string body = "")
        {
            var timestamp = GetTime();

            request.AddHeader("Bitvavo-Access-Key", Environment.GetEnvironmentVariable("BITVAVO_API_KEY"));
            request.AddHeader("Bitvavo-Access-Signature", GenerateSignature(timestamp, method.ToString().ToUpper(), url, body));
            AddTimeStampHeader(request, timestamp);
            request.AddHeader("Content-Type", "application/json");
        }

        private string GetTime()
        {
            Console.WriteLine("Requesting BitVavo GET Time endpoint");
            var requestTime = new RestRequest("time", Method.Get);
            var response_time = Client.Execute(requestTime);
            var jsonData = JsonConvert.DeserializeObject<dynamic>(response_time.Content.ToString());

            var time = jsonData["time"].ToString();
            return time;
        }

        private string GenerateSignature(string timestamp, string method, string url, string body)
        {
            string prehashString = $"{timestamp}{method}/v2/{url}{body}";

            // Console.WriteLine($"hashString: {prehashString}");

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("BITVAVO_API_SECRET"))))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(prehashString));
                var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return signature;
            }
        }

        private void AddTimeStampHeader(RestRequest request, string timestamp = "")
        {
            if (timestamp == "") timestamp = GetTime();
            request.AddHeader("Bitvavo-Access-Timestamp", timestamp.ToString());
        }

        public async Task<decimal> GetPrice(string market)
        {
            try
            {
                Console.WriteLine($"Requesting BitVavo GET price endpoint for market {market}");
                string url = $"ticker/price";
                var request = new RestRequest(url + $"?market={market}", Method.Get);

                var response = await Client.ExecuteAsync(request);
                var price = JsonConvert.DeserializeObject<dynamic>(response.Content).price;
                return (decimal)price;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting price for {market}: {ex.Message}");
            }
        }

        public async Task<List<Quote>> GetCandleData(string market, string interval = "1h", string limit = "100")
        {
            try
            {
                Console.WriteLine($"Requesting BitVavo GET candels endpoint for market {market}");
                var request = new RestRequest($"{market}/candles", Method.Get);
                request.AddParameter("interval", interval);
                request.AddParameter("limit", limit);
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

        public async Task<List<Balance>> GetBalances()
        {
            try
            {
                Console.WriteLine("Requesting BitVavo GET Balances endpoint");
                var url = "balance";
                var method = Method.Get;
                var request = new RestRequest(url, method);
                SetApiRequestHeaders(request, url, method, "");
                var response = await Client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    var balanceResponse = JsonConvert.DeserializeObject<List<Balance>>(response.Content);
                    return balanceResponse;
                }
                else
                {
                    throw new Exception($"Error retrieving balance: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving balance: {ex.Message}");
            }
        }

        private async Task<string> PlaceOrder(string market, string side, decimal amount)
        {
            Console.WriteLine($"Requesting BitVavo POST order endpoint {side} {market}");

            var formattedAmount = FormatAmount(amount, 2);
            if (amount % 1 == 0) { formattedAmount = amount.ToString("N0"); }

            try
            {
                var url = "order";
                var method = Method.Post;
                var body = new
                {
                    market,
                    side,
                    orderType = "market",
                    amountQuote = formattedAmount
                };
                var request = new RestRequest(url, method);
                request.AddJsonBody(body);
                SetApiRequestHeaders(request, url, method, JsonConvert.SerializeObject(body));
                var response = await Client.ExecuteAsync(request);
                if (response.IsSuccessful)
                {
                    var orderResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);
                    return $"Order {side} placed successfully: {orderResponse.orderId}";
                }
                else
                {
                    throw new Exception($"Error placing order: {response.Content}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error placing order: {ex.Message}");
            }
        }

        public async Task<string> Buy(string market, decimal amount)
        {
            return await PlaceOrder(market, "buy", amount);
        }

        public async Task<string> Sell(string market, decimal amount)
        {
            return await PlaceOrder(market, "sell", amount);
        }
        private async Task<dynamic> GetMarketInfo(string market)
        {
            Console.WriteLine($"Requesting BitVavo GET Market endpoint for {market}");
            var url = $"markets/?market={market}";
            var request = new RestRequest(url, Method.Get);
            SetApiRequestHeaders(request, url, Method.Get);

            var response = await Client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<dynamic>(response.Content);
            }
            else
            {
                throw new Exception($"Error retrieving market info: {response.Content}");
            }
        }

        private string FormatAmount(decimal amount, int precision)
        {
            return amount.ToString($"F{precision}", CultureInfo.InvariantCulture);
        }
    }

    public class BitVavoSignature
    {
        public string Timestamp { get; set; }
        public string HttpMethod { get; set; }
        public string Url { get; set; }
        public string Body { get; set; }
    }
}
