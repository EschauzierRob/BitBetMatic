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
            Console.WriteLine($"Saving new thresholds for {thresholds.Strategy} with a score of {thresholds.Highscore}");
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
    public async Task<IndicatorThresholds> GetLatestDecayedThresholdsAsync(string strategy, string market, double decayRate)
    {
        using (var context = new TradingDbContext())
        {
            // Calculate DecayScore based on the age of the thresholds and Highscore
            var bestThresholds = await GetLatestThresholdsAsync(strategy, market);

            if (bestThresholds == null) return null;

            Console.WriteLine($"Raw highscore for {strategy} {market}: {bestThresholds?.Highscore ?? 0}");

            bestThresholds.Highscore = bestThresholds.Highscore * (decimal)Math.Exp(-decayRate * (DateTime.Now - bestThresholds.CreatedAt).TotalDays); // Decay score calculation

            Console.WriteLine($"Decayed highscore for {strategy} {market}: {bestThresholds.Highscore}");

            return bestThresholds; // Return the best thresholds
        }
    }
}