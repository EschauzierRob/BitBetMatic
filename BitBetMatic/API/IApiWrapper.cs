using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace BitBetMatic.API
{
    public interface IApiWrapper
    {
        Task<decimal> GetPrice(string market);
        Task<List<Quote>> GetCandleData(string market, string interval, int limit, DateTime? start = null, DateTime? end = null);
        Task<List<Balance>> GetBalances();
        Task<string> Buy(string market, decimal amount);
        Task<string> Sell(string market, decimal amount);
        Task<List<MarketData>> GetMarkets();
    }
}