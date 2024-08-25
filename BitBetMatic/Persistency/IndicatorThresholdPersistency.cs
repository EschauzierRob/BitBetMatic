using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class IndicatorThresholdPersistency
{
    public async Task InsertThresholdsAsync(IndicatorThresholds thresholds)
    {
        thresholds.Id = 0;
        thresholds.CreatedAt = DateTime.Now;
        await SaveThresholdsAsync(thresholds);
    }
    
    public async Task SaveThresholdsAsync(IndicatorThresholds thresholds)
    {
        using (var context = new TradingDbContext())
        {
            context.IndicatorThresholds.Add(thresholds);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IndicatorThresholds> GetLatestThresholdsAsync(string strategy, string market)
    {
        using (var context = new TradingDbContext())
        {
            return await context.IndicatorThresholds
                .Where(t => t.Strategy == strategy && t.Market == market)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}