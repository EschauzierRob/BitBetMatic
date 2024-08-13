using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitBetMatic.API;
using Microsoft.VisualBasic;
using Skender.Stock.Indicators;

public class DataLoader
{
    private readonly BitvavoApi _api;
    private readonly Dictionary<string, List<Quote>> QuotesPerInterval;

    public DataLoader(BitvavoApi api)
    {
        _api = api;
        QuotesPerInterval = new Dictionary<string, List<Quote>>();
    }

    public async Task<List<Quote>> LoadHistoricalData(string market, string interval, int limit, DateTime start, DateTime end)
    {
        List<Quote> quotes = new List<Quote>();

        while (quotes.Count == 0 || end.Date > start.Date)
        {
            if (QuotesPerInterval.ContainsKey(interval+market) && QuotesPerInterval[interval+market].Count > 0)
            {
                end = QuotesPerInterval[interval+market].Min(x => x.Date);
            }
            quotes = await _api.GetCandleData(market, interval, limit, start, end);

            if (QuotesPerInterval.ContainsKey(interval+market))
            {
                QuotesPerInterval[interval+market].AddRange(quotes);
            }
            else
            {
                QuotesPerInterval[interval+market] = quotes;
            }
        }

        return QuotesPerInterval[interval+market];
    }
}
