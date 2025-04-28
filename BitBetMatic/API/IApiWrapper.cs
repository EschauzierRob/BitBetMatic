using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace BitBetMatic.API
{
    public interface IApiWrapper
    {
        Task<decimal> GetPrice(string market);
        Task<List<FlaggedQuote>> GetCandleData(string market, string interval, int limit, DateTime start, DateTime end);
        Task<List<Balance>> GetBalances();
        Task<string> Buy(string market, decimal amount);
        Task<string> Sell(string market, decimal amount);
        Task<List<MarketData>> GetMarkets();

        Task<List<TradeData>> GetTradeData(string market);
        Task<List<Quote>> GetPortfolioData();
    }

    public class TradeData
    {
        public string Id { get; set; } // "108c3633-0276-4480-a902-17a01829deae",
        public string OrderId { get; set; } // "1d671998-3d44-4df4-965f-0d48bd129a1b",
        public string ClientOrderId { get; set; } // "2be7d0df-d8dc-7b93-a550-8876f3b393e9",
        public long Timestamp { get; set; } // 1542967486256,
        public string Market { get; set; } // "BTC-EUR",
        public string Side { get; set; } // "buy",
        public decimal Amount { get; set; } // "0.005",
        public decimal Price { get; set; } // "5000.1",
        public bool Taker { get; set; } // true,
        public decimal Fee { get; set; } // "0.03",
        public string FeeCurrency { get; set; } // "EUR",
        public bool Settled { get; set; } // true

        public DateTime TimestampAsDateTime => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(Timestamp);
    }
}