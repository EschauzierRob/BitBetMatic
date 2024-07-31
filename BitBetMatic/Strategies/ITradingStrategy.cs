using System.Threading.Tasks;

namespace BitBetMatic
{

    public interface ITradingStrategy
    {
        Task<(BuySellHold Signal, int Score)> AnalyzeMarket(string market);
    }
}