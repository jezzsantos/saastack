using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common.Configuration;
using Common.FeatureFlags;
using Common.Recording;
using Infrastructure.Common.Recording;
using Infrastructure.Hosting.Common;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Workers.Api.Workers;
using Infrastructure.Workers.Api;
using OnPremisesWorkerService.Configuration;
using OnPremisesWorkerService.Core.Abstractions;
using System.Text.Json;
using Common;

namespace OnPremisesWorkerService.Infrastructure.MessageBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        services.AddSingleton<IRabbitMqListenerServiceConnection, RabbitMqListenerServiceConnection>();

        services.AddHttpClient();
        services.AddSingleton<IConfigurationSettings>(new AspNetDynamicConfigurationSettings(configuration));
        services.AddSingleton<IHostSettings, HostSettings>();
        services.AddSingleton<IFeatureFlags, EmptyFeatureFlags>();
        services.AddSingleton(JsonSerializerOptions.Default);
        services.AddSingleton<ICrashReporter>(new NoOpCrashReporter());

        services.AddSingleton<IRecorder>(c => new CrashTraceOnlyRecorder(
            "OnPremises :-: RabbitMq Worker",
            c.GetRequiredService<ILoggerFactory>(),
            c.GetRequiredService<ICrashReporter>()));

        services.AddSingleton<IServiceClientFactory>(c => new InterHostServiceClientFactory(
            c.GetRequiredService<IHttpClientFactory>(),
            c.GetRequiredService<JsonSerializerOptions>(),
            c.GetRequiredService<IHostSettings>()));

        services.AddHostedService<RabbitMqListenerService>();

        return services;
    }

    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IQueueMonitoringApiRelayWorker<UsageMessage>, DeliverUsageRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<AuditMessage>, DeliverAuditRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<EmailMessage>, SendEmailRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<SmsMessage>, SendSmsRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<ProvisioningMessage>, DeliverProvisioningRelayWorker>();
        services
            .AddSingleton<IMessageBusMonitoringApiRelayWorker<DomainEventingMessage>,
                DeliverDomainEventingRelayWorker>();

        return services;
    }
}