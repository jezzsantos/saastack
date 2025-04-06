using OnPremRelay.WorkerHost.Configuration;

namespace OnPremRelay.WorkerHost;

public abstract class Program
{
    public static async Task Main(string[] args)
    {
        // Create the Host for the Worker Service
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.OnPremises.json", false, true);
            })
            .ConfigureServices((hostContext, services) => { })
            .ConfigureServices((hostContext, services) =>
            {
                // Register RabbitMQ settings from configuration
                services.Configure<RabbitMqSettings>(hostContext.Configuration.GetSection("RabbitMQ"));

            })
            .Build();

        await host.RunAsync();
    }
}