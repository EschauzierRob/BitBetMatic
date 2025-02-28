using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitBetMatic.API;
using Skender.Stock.Indicators;

public class DataLoader
{
    private readonly IApiWrapper _api;
    private readonly ConcurrentDictionary<string, List<Quote>> QuotesPerInterval;

    public DataLoader(IApiWrapper api)
    {
        _api = api;
        QuotesPerInterval = new ConcurrentDictionary<string, List<Quote>>();
    }

    public async Task<List<Quote>> LoadHistoricalData(string market, string interval, int limit, DateTime start, DateTime end)
    {
        if (QuotesPerInterval.ContainsKey(interval + market) && QuotesPerInterval[interval + market].Any(q => q.Date == start || q.Date == end))
        {
            return QuotesPerInterval[interval + market];
        }

        List<Quote> quotes = new List<Quote>();

        while (quotes.Count == 0 || end.Date > start.Date)
        {
            if (QuotesPerInterval.ContainsKey(interval + market) && QuotesPerInterval[interval + market].Count > 0)
            {
                end = QuotesPerInterval[interval + market].Min(x => x.Date);
            }
            quotes = await _api.GetCandleData(market, interval, limit, start, end);
            if (quotes == null || quotes.Count == 0)
            {
                break;
            }

            if (QuotesPerInterval.ContainsKey(interval + market))
            {
                QuotesPerInterval[interval + market].AddRange(quotes);
            }
            else
            {
                QuotesPerInterval[interval + market] = quotes;
            }
        }

        return QuotesPerInterval.ContainsKey(interval + market) ? QuotesPerInterval[interval + market] : null;
    }
}
