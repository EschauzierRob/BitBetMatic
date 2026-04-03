using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BitBetMatic.API;
using BitBetMatic.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class CandleRepositoryTests
{
    [Fact]
    public async Task AddCandlesAsync_DoesNotInsertDuplicateMarketDateRows()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var factory = CreateFactory(dbName);
        var repository = new CandleRepository(factory);

        var candle = CreateCandle("BTC-EUR", new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc));

        await repository.AddCandlesAsync(new[] { candle });
        await repository.AddCandlesAsync(new[] { CreateCandle(candle.Market, candle.Date) });

        await using var context = await factory.CreateDbContextAsync();
        Assert.Equal(1, await context.Candles.CountAsync());
    }

    [Fact]
    public async Task GetCandlesAsync_ReturnsSortedCandlesWithinRange()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var factory = CreateFactory(dbName);
        var repository = new CandleRepository(factory);

        await repository.AddCandlesAsync(new List<FlaggedQuote>
        {
            CreateCandle("ETH-EUR", new DateTime(2025, 1, 1, 11, 0, 0, DateTimeKind.Utc)),
            CreateCandle("ETH-EUR", new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc))
        });

        var result = await repository.GetCandlesAsync(
            "ETH-EUR",
            new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Date < result[1].Date);
    }

    private static IDbContextFactory<TradingDbContext> CreateFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new PooledDbContextFactory<TradingDbContext>(options);
    }

    private static FlaggedQuote CreateCandle(string market, DateTime date)
    {
        return new FlaggedQuote
        {
            Market = market,
            Date = date,
            Open = 100,
            High = 110,
            Low = 90,
            Close = 105,
            Volume = 1
        };
    }
}
