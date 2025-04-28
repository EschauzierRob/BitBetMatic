using System.Collections.Generic;
using BitBetMatic.API;

namespace BitBetMatic
{

    public interface ITradingStrategy
    {
        (decimal amount, string action) CalculateOutcome(decimal currentPrice, int score, BuySellHold outcome, PortfolioManager portfolioManager, string market);
        (BuySellHold Signal, int Score) AnalyzeMarket(string market, List<FlaggedQuote> quotes, decimal currentPrice);
        string Interval(); 
        int Limit(); 
    }
}