using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using Skender.Stock.Indicators;

namespace BitBetMatic.API
{
    public class BitvavoApi : IApiWrapper
    {
        private readonly RestClient Client;

        public BitvavoApi()
        {
            Client = new RestClient(Environment.GetEnvironmentVariable("API_BASE_URL"));
        }

        private void SetApiRequestHeaders(RestRequest request, string url, string body = "")
        {
            var timestamp = GetTime();

            request.AddHeader("Bitvavo-Access-Key", Environment.GetEnvironmentVariable("BITVAVO_API_KEY"));
            request.AddHeader("Bitvavo-Access-Signature", GenerateSignature(timestamp, request.Method.ToString().ToUpper(), url, body));
            AddTimeStampHeader(request, timestamp);
            request.AddHeader("Bitvavo-Access-Window", 60000);
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

        public async Task<List<FlaggedQuote>> GetCandleData(string market, string interval, int limit, DateTime start, DateTime end)
        {
            try
            {
                using (var context = new TradingDbContext())
                {
                    Console.WriteLine($"Fetching candle data from database for {market}: {start} - {end}...");

                    // Zorg dat start en end in UTC zijn
                    start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                    end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

                    // Ophalen van bestaande candles in de gevraagde periode
                    var existingQuotes = GetExistingQuotes(context, market, start, end).Result;

                    DateTime? earliestStoredCandle = existingQuotes.Keys.Count > 0 ? new DateTime(existingQuotes.Keys.Min()) : null;
                    DateTime? latestStoredCandle = existingQuotes.Keys.Count > 0 ? new DateTime(existingQuotes.Keys.Max()) : null;

                    var newQuotes = new List<FlaggedQuote>();

                    // Stap 1: Backfill oudere candles als die ontbreken
                    if (earliestStoredCandle == null || earliestStoredCandle > start)
                    {
                        DateTime fetchStart = start;
                        DateTime fetchEnd = earliestStoredCandle?.AddSeconds(-1) ?? end; // Haal alleen ontbrekende op

                        var fromBitVavo = await FetchCandlesFromBitVavo(market, interval, limit, fetchStart, fetchEnd);
                        existingQuotes = GetExistingQuotes(context, market, start, end).Result;
                        var newNewQuotes = fromBitVavo.Where(x => !existingQuotes.ContainsKey(x.Date.Ticks)).ToList();

                        newQuotes.AddRange(newNewQuotes);

                        // Update latestStoredCandle na toevoegen nieuwe data
                        if (newNewQuotes.Count > 0)
                            latestStoredCandle = newNewQuotes.Max(x => x.Date);
                    }

                    // Stap 2: Recente candles ophalen
                    if (latestStoredCandle == null || latestStoredCandle < end)
                    {
                        DateTime fetchStart = latestStoredCandle?.AddSeconds(1) ?? start;

                        var fromBitVavo = await FetchCandlesFromBitVavo(market, interval, limit, fetchStart, end);
                        existingQuotes = GetExistingQuotes(context, market, start, end).Result;
                        var newNewQuotes = fromBitVavo.Where(x => !existingQuotes.ContainsKey(x.Date.Ticks)).ToList();

                        newQuotes.AddRange(newNewQuotes);
                    }


                    // 🔹 **Stap 3: Opslaan in database**
                    if (newQuotes.Count > 0)
                    {
                        await context.Candles.AddRangeAsync(newQuotes);
                        await context.SaveChangesAsync();
                        Console.WriteLine($"Stored {newQuotes.Count} new candles in database.");
                    } else {
                        Console.WriteLine("No new candles from BitVavo to store...");
                    }

                    // Voeg nieuwe candles toe aan existingQuotes en return alles
                    foreach (var quote in newQuotes)
                    {
                        existingQuotes[quote.Date.Ticks] = quote;
                    }

                    return existingQuotes.Values.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting candle data for {market}: {ex.Message}");
                throw;
            }
        }

        private async Task<Dictionary<long, FlaggedQuote>> GetExistingQuotes(TradingDbContext context, string market, DateTime start,DateTime end) {
            return await context.Candles
                        .Where(x => x.Market == market && x.Date >= start && x.Date <= end)
                        .ToDictionaryAsync(x => x.Date.Ticks);
        }

        private async Task<List<FlaggedQuote>> FetchCandlesFromBitVavo(string market, string interval, int limit, DateTime start, DateTime end)
        {
            var newQuotes = new List<FlaggedQuote>();

            while (start < end)
            {
                Console.WriteLine($"Fetching candles from BitVavo: {start} - {end}");

                start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

                limit = Math.Min(limit, Get15MinuteIntervals(start, end));
                if (limit == 0) { return newQuotes; }

                var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(end, DateTimeKind.Utc);

                var request = new RestRequest($"{market}/candles", Method.Get);
                request.AddParameter("interval", interval);
                request.AddParameter("limit", limit);
                request.AddParameter("start", new DateTimeOffset(startUtc).ToUnixTimeMilliseconds());
                request.AddParameter("end", new DateTimeOffset(endUtc).ToUnixTimeMilliseconds());

                var response = await Client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"Warning: Error from BitVavo: {response.Content}");
                    PrintRestRequest(request);
                }

                var candles = JsonConvert.DeserializeObject<dynamic>(response.Content);
                if (candles == null || candles.Count == 0)
                {
                    Console.WriteLine("No new candles found.");
                    break;
                }

                foreach (var candle in candles)
                {
                    var date = DateTimeOffset.FromUnixTimeMilliseconds((long)candle[0]).UtcDateTime; // Tijdzone fix
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                    var quote = new FlaggedQuote
                    {
                        Date = date,
                        Open = (decimal)candle[1],
                        High = (decimal)candle[2],
                        Low = (decimal)candle[3],
                        Close = (decimal)candle[4],
                        Volume = (decimal)candle[5],
                        Market = market
                    };

                    newQuotes.Add(quote);
                }

                if (newQuotes.Count == 0)
                {
                    Console.WriteLine("All fetched candles already exist. Exiting loop.");
                    break;
                }

                // Start volgende fetch vanaf de laatst opgehaalde candle
                end = newQuotes.Min(q => q.Date).AddSeconds(-1);
            }

            return newQuotes;
        }

        public async Task<List<Balance>> GetBalances()
        {
            try
            {
                Console.WriteLine("Requesting BitVavo GET Balances endpoint");
                var url = "balance";
                var method = Method.Get;
                var request = new RestRequest(url, method);
                SetApiRequestHeaders(request, url, "");
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
                SetApiRequestHeaders(request, url, JsonConvert.SerializeObject(body));
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
            SetApiRequestHeaders(request, url);

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

        public async Task<List<MarketData>> GetMarkets()
        {
            var client = new RestClient("https://edge.bitvavo.com");
            var request = new RestRequest("exchange/proxy/v3/markets/data", Method.Get);
            request.AddParameter("miniChart", "false");

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Error retrieving market data: {response.Content}");
            }

            var marketDataList = JsonConvert.DeserializeObject<List<MarketData>>(response.Content);
            return marketDataList;
        }


        public async Task<List<TradeData>> GetTradeData(string market)
        {
            Console.WriteLine($"Requesting BitVavo GET Trades endpoint for {market}");
            var url = $"trades/?market={market}";
            var request = new RestRequest(url, Method.Get);
            SetApiRequestHeaders(request, url);

            var response = await Client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Error retrieving trades data: {response.Content}");
            }

            var result = JsonConvert.DeserializeObject<List<TradeData>>(response.Content);
            return result;
        }

        public Task<List<Quote>> GetPortfolioData()
        {
            throw new NotImplementedException();
        }

        private string FormatAmount(decimal amount, int precision)
        {
            return amount.ToString($"F{precision}", CultureInfo.InvariantCulture);
        }

        private void PrintRestRequest(RestRequest request)
        {
            Console.WriteLine($"[DEBUG] Request: {request.Method} {request.Resource}");

            foreach (var parameter in request.Parameters)
            {
                Console.WriteLine($"  - {parameter.Name}: {parameter.Value}");
            }
        }

        static int Get15MinuteIntervals(DateTime start, DateTime end)
        {
            if (end < start)
                throw new ArgumentException("End time must be after start time");

            TimeSpan difference = end - start;
            return (int)(difference.TotalMinutes / 15);
        }
    }

    public class FlaggedQuote : Quote
    {
        public BuySellHold TradeAction { get; set; }
        public int Id { get; set; }
        public string Market { get; set; }

        public FlaggedQuote()
        {
            TradeAction = BuySellHold.Hold;
        }

        public FlaggedQuote(Quote quote)
        {
            TradeAction = BuySellHold.Hold;
            Open = quote.Open;
            Close = quote.Close;
            High = quote.High;
            Low = quote.Low;
            Date = quote.Date;
            Volume = quote.Volume;
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
