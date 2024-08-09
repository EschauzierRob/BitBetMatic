using System.Collections.Generic;
using System.Threading.Tasks;
using Skender.Stock.Indicators;

namespace BitBetMatic
{

    public interface ITradingStrategy
    {
        (decimal amount, string action) CalculateOutcome(decimal currentPrice, int score, BuySellHold outcome, PortfolioManager portfolioManager, string market);
        (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<Quote> quotes, decimal currentPrice);
        string Interval(); 
        int Limit(); 
    }
}