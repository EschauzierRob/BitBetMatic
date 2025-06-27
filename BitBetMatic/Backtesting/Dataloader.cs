using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitBetMatic.API;
using BitBetMatic.Repositories;

public class DataLoader
{
    private readonly IApiWrapper API;
    private readonly ICandleRepository CandleRepository;

    public DataLoader(IApiWrapper api, ICandleRepository candleRepository)
    {
        API = api;
        CandleRepository = candleRepository;
    }

    public async Task<List<FlaggedQuote>> LoadHistoricalData(string market, string interval, int limit, DateTime start, DateTime end)
    {

        List<FlaggedQuote> quotes;
        try
        {
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

            Console.WriteLine($"Fetching candle data from database for {market}: {start} - {end}...");

            var existingQuotes = (await CandleRepository.GetCandlesAsync(market, start, end))
                .ToDictionary(x => x.Date.Ticks);

            DateTime? earliestStoredCandle = existingQuotes.Count > 0 ? new DateTime(existingQuotes.Keys.Min()) : null;
            DateTime? latestStoredCandle = existingQuotes.Count > 0 ? new DateTime(existingQuotes.Keys.Max()) : null;

            var newQuotes = new List<FlaggedQuote>();

            if (earliestStoredCandle == null || earliestStoredCandle > start)
            {
                DateTime fetchStart = start;
                DateTime fetchEnd = earliestStoredCandle?.AddSeconds(-1) ?? end;

                var fromBitVavo = await API.GetCandleData(market, interval, limit, fetchStart, fetchEnd);
                existingQuotes = (await CandleRepository.GetCandlesAsync(market, start, end))
                    .ToDictionary(x => x.Date.Ticks);
                var newNewQuotes = fromBitVavo.Where(x => !existingQuotes.ContainsKey(x.Date.Ticks)).ToList();

                newQuotes.AddRange(newNewQuotes);

                if (newNewQuotes.Count > 0)
                    latestStoredCandle = newNewQuotes.Max(x => x.Date);
            }

            if (latestStoredCandle == null || latestStoredCandle < end)
            {
                DateTime fetchStart = latestStoredCandle?.AddSeconds(1) ?? start;

                var fromBitVavo = await API.GetCandleData(market, interval, limit, fetchStart, end);
                existingQuotes = (await CandleRepository.GetCandlesAsync(market, start, end))
                    .ToDictionary(x => x.Date.Ticks);

                var newNewQuotes = fromBitVavo.Where(x => !existingQuotes.ContainsKey(x.Date.Ticks)).ToList();

                newQuotes.AddRange(newNewQuotes);
            }

            if (newQuotes.Count > 0)
            {
                await CandleRepository.AddCandlesAsync(newQuotes);
                Console.WriteLine($"Stored {newQuotes.Count} new candles in database.");
            }
            else
            {
                Console.WriteLine("No new candles from BitVavo to store...");
            }

            foreach (var quote in newQuotes)
            {
                existingQuotes[quote.Date.Ticks] = quote;
            }

            quotes = existingQuotes.Values.OrderBy(q => q.Date).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting candle data for {market}: {ex.Message}");
            throw;
        }

        return quotes;
    }
}
