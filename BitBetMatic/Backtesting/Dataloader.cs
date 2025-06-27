using System;
using System.Collections.Generic;
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

        var quotes = await CandleRepository.GetCandlesAsync(market, start, end);

        if (quotes.Count == 0)
        {
            quotes = await API.GetCandleData(market, interval, limit, start, end);
            await CandleRepository.AddCandlesAsync(quotes);
        }

        return quotes;
    }
}
