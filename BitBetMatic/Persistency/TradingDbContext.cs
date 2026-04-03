using System;
using BitBetMatic.API;
using Microsoft.EntityFrameworkCore;

public class TradingDbContext : DbContext
{
    public const string ConnectionStringEnvironmentVariable = "DB_CONNECTION_STRING";

    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    // Parameterless constructor is kept for existing call sites and EF design-time tooling.
    public TradingDbContext()
    {
    }

    public DbSet<IndicatorThresholds> IndicatorThresholds { get; set; }
    public DbSet<FlaggedQuote> Candles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = GetRequiredConnectionString();
        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IndicatorThresholds>()
            .HasIndex(e => new { e.Strategy, e.Market, e.CreatedAt });

    }

    public static string GetRequiredConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Database connection string is not set. Configure environment variable '{ConnectionStringEnvironmentVariable}'.");
        }

        return connectionString;
    }
}
