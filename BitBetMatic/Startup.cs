using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BitBetMatic.API;
using BitBetMatic;
using System;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register DbContext
        services.AddDbContext<TradingDbContext>(options =>
            options.UseSqlServer(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")));

        // Register API wrapper and processors
        services.AddScoped<IApiWrapper, BitvavoApi>();
        services.AddScoped<BitBetMaticProcessor>();
        services.AddScoped<BackTesting>();
        services.AddScoped<DataLoader>();
        services.AddScoped<IndicatorThresholdPersistency>();
    }
}
