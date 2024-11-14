#if TESTINGONLY
using System.Reflection;
using System.Text.Json;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common.Extensions;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Api.Operations.Shared.EventNotifications;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Interfaces.Clients;
using TestingStubApiHost.Api;
using TestingStubApiHost.Workers;

namespace TestingStubApiHost;

public class StubApiModule : ISubdomainModule
{
    private static readonly Dictionary<string, IWebRequest> MessageBusTopicMappings = new()
    {
        //EXTEND: add mappings to any new topics here to monitor
        { WorkerConstants.MessageBuses.Topics.DomainEvents, new DrainAllDomainEventsRequest() }
    };
    private static readonly Dictionary<string, IWebRequest> QueuedMappings = new()
    {
        //EXTEND: add mappings to any new queues here to monitor
        { WorkerConstants.Queues.Audits, new DrainAllAuditsRequest() },
        { WorkerConstants.Queues.Usages, new DrainAllUsagesRequest() },
        { WorkerConstants.Queues.Emails, new DrainAllEmailsRequest() },
        { WorkerConstants.Queues.Smses, new DrainAllSmsesRequest() },
        { WorkerConstants.Queues.Provisionings, new DrainAllProvisioningsRequest() }
    };

    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get
        {
            return (app, middlewares) =>
            {
                app.RegisterRoutes();

#if TESTINGONLY
                var stubDrainingServices = app.Services.GetServices<IHostedService>()
                    .OfType<StubCloudWorkerService>()
                    .ToList();
                if (stubDrainingServices.HasAny())
                {
                    var stubDrainingService = stubDrainingServices[0];
                    var queues = stubDrainingService.MonitoredQueues.Join(", ");
                    var topics = stubDrainingService.MonitoredBusTopics.Join(", ");
                    middlewares.Add(new MiddlewareRegistration(-40, _ =>
                        {
                            //Nothing to register
                        },
                        "Feature: Background worker for message draining is enabled, on: queues -> {Queues}, and bus topics -> {Topics}",
                        queues,
                        topics));
                }
#endif
            };
        }
    }

    public Assembly DomainAssembly => typeof(StubHelloApi).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new();

    public Assembly InfrastructureAssembly => typeof(StubHelloApi).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (configuration, services) =>
            {
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddSimpleConsole(options =>
                    {
                        options.TimestampFormat = "hh:mm:ss ";
                        options.SingleLine = true;
                        options.IncludeScopes = false;
                    });
                    builder.AddDebug();
                    builder.AddEventSourceLogger();
                });

                services.AddSingleton<IServiceClient>(c =>
                    new InterHostServiceClient(c.GetRequiredService<IHttpClientFactory>(),
                        c.GetRequiredService<JsonSerializerOptions>(),
                        c.GetRequiredService<IHostSettings>().GetAncillaryApiHostBaseUrl()));
                services.AddSingleton<IMessageMonitor, StubMessageMonitor>();
                services.AddHostedService(c =>
                    new StubCloudWorkerService(c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageMonitor>(),
                        c.GetRequiredServiceForPlatform<LocalMachineJsonFileStore>(),
                        c.GetRequiredService<IHttpClientFactory>(),
                        c.GetRequiredService<JsonSerializerOptions>(),
                        c.GetRequiredService<ILogger<StubCloudWorkerService>>(),
                        QueuedMappings,
                        MessageBusTopicMappings));
            };
        }
    }
}
#endif