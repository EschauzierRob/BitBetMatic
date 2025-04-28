using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BitBetMatic.API;

public class DataLoader
{
    private readonly IApiWrapper _api;

    public DataLoader(IApiWrapper api)
    {
        _api = api;
    }

    public async Task<List<FlaggedQuote>> LoadHistoricalData(string market, string interval, int limit, DateTime start, DateTime end)
    {

        var quotes = await _api.GetCandleData(market, interval, limit, start, end);
            return quotes;
    }
}
