using System.Text.Json;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Configuration;
using Common.FeatureFlags;
using Common.Recording;
using Infrastructure.Common.Recording;
using Infrastructure.Hosting.Common;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Interfaces.Clients;
using Infrastructure.Workers.Api;
using Infrastructure.Workers.Api.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if !TESTINGONLY
using Microsoft.ApplicationInsights;
#endif

namespace AzureFunctions.Api.WorkerHost;

public static class HostExtensions
{
    public static void AddDependencies(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddHttpClient();
        services.AddSingleton<IConfigurationSettings>(new AspNetDynamicConfigurationSettings(context.Configuration));
        services.AddSingleton<IHostSettings, HostSettings>();
        services.AddSingleton<IFeatureFlags, EmptyFeatureFlags>();
        services.AddSingleton(JsonSerializerOptions.Default);

#if TESTINGONLY
        services.AddSingleton<ICrashReporter>(new NoOpCrashReporter());
#else
#if HOSTEDONAZURE
        //Note: ApplicationInsights TelemetryClient is not added by default by the runtime V4
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddSingleton<ICrashReporter>(c =>
            new ApplicationInsightsCrashReporter(c.GetRequiredService<TelemetryClient>()));
#endif
#endif

        services.AddSingleton<IRecorder>(c =>
            new CrashTraceOnlyRecorder("Azure API Workers", c.GetRequiredService<ILoggerFactory>(),
                c.GetRequiredService<ICrashReporter>()));
        services.AddSingleton<IServiceClient>(c =>
            new InterHostServiceClient(c.GetRequiredService<IHttpClientFactory>(),
                c.GetRequiredService<JsonSerializerOptions>(),
                c.GetRequiredService<IHostSettings>().GetAncillaryApiHostBaseUrl()));
        services.AddSingleton<IServiceClientFactory>(c =>
            new InterHostServiceClientFactory(c.GetRequiredService<IHttpClientFactory>(),
                c.GetRequiredService<JsonSerializerOptions>()));
        services.AddSingleton<IQueueMonitoringApiRelayWorker<UsageMessage>, DeliverUsageRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<AuditMessage>, DeliverAuditRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<EmailMessage>, SendEmailRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<SmsMessage>, SendSmsRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<ProvisioningMessage>, DeliverProvisioningRelayWorker>();
        services
            .AddSingleton<IMessageBusMonitoringApiRelayWorker<DomainEventingMessage>,
                DeliverDomainEventingRelayWorker>();
    }
}