using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TradingDbContext>();
        optionsBuilder.UseSqlServer(TradingDbContext.GetRequiredConnectionString());

        return new TradingDbContext(optionsBuilder.Options);
    }
}
