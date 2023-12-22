using AzureFunctions.Api.WorkerHost;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration(builder =>
    {
        builder
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile("appsettings.local.json", true, false)
            .AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) => { services.AddDependencies(context); })
    .Build();

host.Run();

namespace AzureFunctions.Api.WorkerHost
{
    [UsedImplicitly]
    public class Program
    {
    }
}