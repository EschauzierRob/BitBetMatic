using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitBetMatic.API;
using Microsoft.EntityFrameworkCore;

namespace BitBetMatic.Repositories
{
    public class CandleRepository
    {
        public async Task<List<FlaggedQuote>> GetCandlesAsync(string market, DateTime start, DateTime end)
        {
            using var context = new TradingDbContext();
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

            return await context.Candles
                .Where(c => c.Market == market && c.Date >= start && c.Date <= end)
                .OrderBy(c => c.Date)
                .ToListAsync();
        }

        public async Task AddCandlesAsync(IEnumerable<FlaggedQuote> candles)
        {
            using var context = new TradingDbContext();
            await context.Candles.AddRangeAsync(candles);
            await context.SaveChangesAsync();
        }
    }
}
