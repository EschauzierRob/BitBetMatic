using System;

public class RiskOverlay
{
    private decimal StaticStopLossThreshold;
    private decimal TrailingStopLossPercentage;
    private decimal HighestPrice;
    private decimal StopLossPrice;
    private decimal EntryPrice;

    public RiskOverlay(decimal staticStopLossThreshold, decimal trailingStopLossPercentage)
    {
        StaticStopLossThreshold = staticStopLossThreshold;
        TrailingStopLossPercentage = trailingStopLossPercentage;
        HighestPrice = 0;
        StopLossPrice = 0;
        EntryPrice = 0;
    }

    public void UpdateEntryPrice(decimal currentPrice)
    {
        EntryPrice = currentPrice;
    }

    public void UpdatePrice(decimal currentPrice)
    {
        if (EntryPrice == 0)
        {
            EntryPrice = currentPrice;
        }

        // Update highest price
        if (currentPrice > HighestPrice)
        {
            HighestPrice = currentPrice;
            StopLossPrice = HighestPrice * (1 - TrailingStopLossPercentage / 100);
        }

        // Ensure stop-loss is never below the static threshold
        decimal staticStopLossLevel = EntryPrice * (1 - StaticStopLossThreshold / 100);
        StopLossPrice = Math.Max(StopLossPrice, staticStopLossLevel);
    }

    public bool ShouldSell(decimal currentPrice)
    {
        return currentPrice <= StopLossPrice;
    }
}