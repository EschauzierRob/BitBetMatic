using System;
using BitBetMatic.API;
using Microsoft.EntityFrameworkCore;

public class TradingDbContext : DbContext
{
    public DbSet<IndicatorThresholds> IndicatorThresholds { get; set; }
    public DbSet<FlaggedQuote> Candles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = "Server=tcp:bitbetmatic-db.database.windows.net,1433;Initial Catalog=bitbetmatic-db;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";";
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")))
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
        modelBuilder.Entity<IndicatorThresholds>()
            .HasIndex(e => new { e.Strategy, e.Market, e.CreatedAt });
    }
}