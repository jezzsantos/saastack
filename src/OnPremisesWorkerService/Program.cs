using OnPremisesWorkerService.Infrastructure.MessageBus;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.OnPremises.json", true);

builder.Services
    .AddRabbitMqInfrastructure(builder.Configuration)
    .AddFeatureHandlers();

await builder.Build().RunAsync();