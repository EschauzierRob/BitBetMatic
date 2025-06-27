using BitBetMatic.API;
using BitBetMatic.Repositories;
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
        services.AddTransient<ICandleRepository, CandleRepository>();
        services.AddTransient<IApiWrapper, BitvavoApi>();
    })
    .Build();
    
host.Run();
