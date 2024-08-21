using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class IndicatorThresholdPersistency
{
    private readonly TradingDbContext _dbContext;

    public IndicatorThresholdPersistency()
    {
        _dbContext = Startup.ServiceProvider.GetService<TradingDbContext>();
    }
    public async Task SaveThresholdsAsync(IndicatorThresholdsEntity thresholds)
    {
        _dbContext.IndicatorThresholds.Add(thresholds);
        await _dbContext.SaveChangesAsync();
    }
    public async Task<IndicatorThresholdsEntity> GetLatestThresholdsAsync(string strategy, string market)
    {
        return await _dbContext.IndicatorThresholds
            .Where(t => t.Strategy == strategy && t.Market == market)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

}