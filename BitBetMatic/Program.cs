using BitBetMatic.API;
using BitBetMatic.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging(logging =>
    {
        // logging.AddConsole();
    })
    .ConfigureServices(services =>
    {
        services.AddDbContextFactory<TradingDbContext>(options =>
            options.UseSqlServer(TradingDbContext.GetRequiredConnectionString()));

        services.AddHostedService<MigrationHostedService>();

        services.AddTransient<ICandleRepository, CandleRepository>();
        services.AddTransient<IApiWrapper, BitvavoApi>();
    })
    .Build();

host.Run();
