using System.Collections.Generic;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace BitBetMatic.API
{
    public interface IApiWrapper
    {
        Task<decimal> GetPrice(string market);
        Task<List<Quote>> GetCandleData(string market, string interval = "1h", string limit = "100");
        Task<List<Balance>> GetBalances();
        Task<string> Buy(string market, decimal amount);
        Task<string> Sell(string market, decimal amount);
        Task<List<MarketData>> GetMarkets();
    }
}