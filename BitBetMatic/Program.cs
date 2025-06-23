using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(Startup.ConfigureServices)
    .ConfigureLogging(logging =>
    {
        // logging.AddConsole();
    })
    .Build();

host.Run();
