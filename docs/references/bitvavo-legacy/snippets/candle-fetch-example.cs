// Reference-only legacy snippet.
// Extracted from BitBetMatic v1 for documentation and migration planning.
// Not part of the active runtime for BitBetMatic 2.0.

public async Task<List<FlaggedQuote>> GetCandleData(string market, string interval, int limit, DateTime start, DateTime end)
{
    var newQuotes = new List<FlaggedQuote>();

    while (start < end)
    {
        limit = Math.Min(limit, Get15MinuteIntervals(start, end));
        if (limit == 0) return newQuotes;

        var request = new RestRequest($"{market}/candles", Method.Get);
        request.AddParameter("interval", interval);
        request.AddParameter("limit", limit);
        request.AddParameter("start", new DateTimeOffset(start).ToUnixTimeMilliseconds());
        request.AddParameter("end", new DateTimeOffset(end).ToUnixTimeMilliseconds());

        var response = await Client.ExecuteAsync(request);
        var candles = JsonConvert.DeserializeObject<dynamic>(response.Content);
        if (candles == null || candles.Count == 0) break;

        foreach (var candle in candles)
        {
            var date = DateTimeOffset.FromUnixTimeMilliseconds((long)candle[0]).UtcDateTime;
            newQuotes.Add(new FlaggedQuote
            {
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Open = (decimal)candle[1],
                High = (decimal)candle[2],
                Low = (decimal)candle[3],
                Close = (decimal)candle[4],
                Volume = (decimal)candle[5],
                Market = market
            });
        }

        end = newQuotes.Min(q => q.Date).AddSeconds(-1);
    }

    return newQuotes;
}
