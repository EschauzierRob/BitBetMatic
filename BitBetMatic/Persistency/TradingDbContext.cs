using System;
using Microsoft.EntityFrameworkCore;

public class TradingDbContext : DbContext
{
    public DbSet<IndicatorThresholdsEntity> IndicatorThresholds { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not set.");
        }
        // Verbind met Azure SQL Database
        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Indexen voor betere query-prestaties
        modelBuilder.Entity<IndicatorThresholdsEntity>()
            .HasIndex(e => new { e.Strategy, e.Market, e.CreatedAt });
    }
}