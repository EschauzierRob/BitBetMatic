using BitBetMatic.API;
using System.Threading.Tasks;

namespace BitBetMatic
{
    public abstract class TradingStrategyBase : ITradingStrategy
    {
        protected readonly BitvavoApi Api;

        public TradingStrategyBase(BitvavoApi api)
        {
            Api = api;
        }

        public abstract Task<(BuySellHold Signal, int Score)> AnalyzeMarket(string market);
    }
}
