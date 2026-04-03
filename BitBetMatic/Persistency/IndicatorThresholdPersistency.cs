using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class IndicatorThresholdPersistency
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public IndicatorThresholdPersistency() : this(new PooledDbContextFactory<TradingDbContext>(
        new DbContextOptionsBuilder<TradingDbContext>()
            .UseSqlServer(TradingDbContext.GetRequiredConnectionString())
            .Options))
    {
    }

    public IndicatorThresholdPersistency(IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task InsertThresholdsAsync(IndicatorThresholds thresholds)
    {
        thresholds.Id = 0;
        thresholds.CreatedAt = DateTime.UtcNow;
        await SaveThresholdsAsync(thresholds);
    }

    public async Task SaveThresholdsAsync(IndicatorThresholds thresholds)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        Console.WriteLine($"Saving new thresholds for {thresholds.Strategy} with a score of {thresholds.Highscore}");
        context.IndicatorThresholds.Add(thresholds);
        await context.SaveChangesAsync();
    }

    public async Task<IndicatorThresholds> GetLatestThresholdsAsync(string strategy, string market)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.IndicatorThresholds
            .Where(t => t.Strategy == strategy && t.Market == market)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IndicatorThresholds> GetLatestDecayedThresholdsAsync(string strategy, string market, double decayRate)
    {
        var bestThresholds = await GetLatestThresholdsAsync(strategy, market);

        if (bestThresholds == null)
        {
            return null;
        }

        Console.WriteLine($"Raw highscore for {strategy} {market}: {bestThresholds.Highscore}");

        bestThresholds.Highscore *= (decimal)Math.Exp(-decayRate * (DateTime.UtcNow - bestThresholds.CreatedAt).TotalDays);

        Console.WriteLine($"Decayed highscore for {strategy} {market}: {bestThresholds.Highscore}");

        return bestThresholds;
    }
}
