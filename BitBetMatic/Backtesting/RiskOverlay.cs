using System;

public class RiskOverlay
{
    private decimal staticStopLossThreshold;
    private decimal trailingStopLossPercentage;
    private decimal highestPrice;
    private decimal stopLossPrice;
    private decimal entryPrice;

    public RiskOverlay(decimal staticStopLossThreshold, decimal trailingStopLossPercentage)
    {
        this.staticStopLossThreshold = staticStopLossThreshold;
        this.trailingStopLossPercentage = trailingStopLossPercentage;
        highestPrice = 0;
        stopLossPrice = 0;
        entryPrice = 0;
    }

    public void UpdateEntryPrice(decimal currentPrice)
    {
        entryPrice = currentPrice;
    }

    public void UpdatePrice(decimal currentPrice)
    {
        if (entryPrice == 0)
        {
            entryPrice = currentPrice;
        }

        // Update highest price
        if (currentPrice > highestPrice)
        {
            highestPrice = currentPrice;
            stopLossPrice = highestPrice * (1 - trailingStopLossPercentage / 100);
        }

        // Ensure stop-loss is never below the static threshold
        decimal staticStopLossLevel = entryPrice * (1 - staticStopLossThreshold / 100);
        stopLossPrice = Math.Max(stopLossPrice, staticStopLossLevel);
    }

    public bool ShouldSell(decimal currentPrice)
    {
        return currentPrice <= stopLossPrice;
    }
}