using System;
using Xunit;

public class TradingDbContextTests
{
    [Fact]
    public void GetRequiredConnectionString_ThrowsWhenMissing()
    {
        var key = TradingDbContext.ConnectionStringEnvironmentVariable;
        var previous = Environment.GetEnvironmentVariable(key);

        try
        {
            Environment.SetEnvironmentVariable(key, null);
            Assert.Throws<InvalidOperationException>(() => TradingDbContext.GetRequiredConnectionString());
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previous);
        }
    }
}
