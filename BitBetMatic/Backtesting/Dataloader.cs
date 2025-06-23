using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BitBetMatic.API;
using BitBetMatic.Repositories;

public class DataLoader
{
    private readonly IApiWrapper _api;
    private readonly CandleRepository _candleRepository;

    public DataLoader(IApiWrapper api, CandleRepository candleRepository)
    {
        _api = api;
        _candleRepository = candleRepository;
    }

    public async Task<List<FlaggedQuote>> LoadHistoricalData(string market, string interval, int limit, DateTime start, DateTime end)
    {

        var quotes = await _candleRepository.GetCandlesAsync(market, start, end);

        if (quotes.Count == 0)
        {
            quotes = await _api.GetCandleData(market, interval, limit, start, end);
            await _candleRepository.AddCandlesAsync(quotes);
        }

        return quotes;
    }
}
