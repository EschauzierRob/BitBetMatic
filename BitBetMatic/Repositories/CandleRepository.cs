using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitBetMatic.API;
using Microsoft.EntityFrameworkCore;

namespace BitBetMatic.Repositories
{
    public interface ICandleRepository
    {
        Task<List<FlaggedQuote>> GetCandlesAsync(string market, DateTime start, DateTime end);
        Task AddCandlesAsync(IEnumerable<FlaggedQuote> candles);
    }

    public class CandleRepository : ICandleRepository
    {
        private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

        public CandleRepository(IDbContextFactory<TradingDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<FlaggedQuote>> GetCandlesAsync(string market, DateTime start, DateTime end)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

            return await context.Candles
                .Where(c => c.Market == market && c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date)
                .ToListAsync();
        }

        public async Task AddCandlesAsync(IEnumerable<FlaggedQuote> candles)
        {
            var incomingCandles = candles
                .Where(c => c is not null)
                .Select(c =>
                {
                    c.Date = DateTime.SpecifyKind(c.Date, DateTimeKind.Utc);
                    return c;
                })
                .ToList();

            if (incomingCandles.Count == 0)
            {
                return;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync();

            var marketGroups = incomingCandles
                .Where(c => !string.IsNullOrWhiteSpace(c.Market))
                .GroupBy(c => c.Market);

            var existingByMarket = new Dictionary<string, HashSet<DateTime>>();
            foreach (var group in marketGroups)
            {
                var minDate = group.Min(c => c.Date);
                var maxDate = group.Max(c => c.Date);

                var existingDates = await context.Candles
                    .Where(c => c.Market == group.Key && c.Date >= minDate && c.Date <= maxDate)
                    .Select(c => c.Date)
                    .ToListAsync();

                existingByMarket[group.Key] = existingDates.ToHashSet();
            }

            var candlesToAdd = incomingCandles
                .Where(c => !string.IsNullOrWhiteSpace(c.Market))
                .Where(c => !existingByMarket[c.Market].Contains(c.Date))
                .ToList();

            if (candlesToAdd.Count == 0)
            {
                return;
            }

            await context.Candles.AddRangeAsync(candlesToAdd);
            await context.SaveChangesAsync();
        }
    }
}
