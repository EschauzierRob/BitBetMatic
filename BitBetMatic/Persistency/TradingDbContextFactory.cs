using Microsoft.EntityFrameworkCore.Design;

public class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        return new TradingDbContext();
    }
}
