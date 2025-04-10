using System.Text.Json;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Configuration;
using Common.FeatureFlags;
using Common.Recording;
using Infrastructure.Common.Recording;
using Infrastructure.Hosting.Common;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Workers.Api;
using Infrastructure.Workers.Api.Workers;
using OnPremRelay.WorkerHost.Configuration;
using OnPremRelay.WorkerHost.Messaging.Delivery;
using OnPremRelay.WorkerHost.Messaging.Interfaces;
using OnPremRelay.WorkerHost.Messaging.Services;
using OnPremRelay.WorkerHost.Workers;

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
            .ConfigureServices((hostContext, services) =>
            {
                // Register RabbitMQ settings from configuration
                services.Configure<RabbitMqSettings>(hostContext.Configuration.GetSection("RabbitMQ"));

                // Register infrastructure services
                services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
                services.AddSingleton<IMessageBrokerService, RabbitMqBrokerService>();

                services.AddHttpClient();
                services.AddSingleton<IConfigurationSettings>(
                    new AspNetDynamicConfigurationSettings(hostContext.Configuration));
                services.AddSingleton<IHostSettings, HostSettings>();
                services.AddSingleton<IFeatureFlags, EmptyFeatureFlags>();
                services.AddSingleton(JsonSerializerOptions.Default);

                services.AddSingleton<ICrashReporter>(new NoOpCrashReporter());
                services.AddSingleton<IRecorder>(c => new CrashTraceOnlyRecorder("OnPremises Api Workers",
                    c.GetRequiredService<ILoggerFactory>(), c.GetRequiredService<ICrashReporter>()));
                services.AddSingleton<IServiceClientFactory>(c => new InterHostServiceClientFactory(
                    c.GetRequiredService<IHttpClientFactory>(),
                    c.GetRequiredService<JsonSerializerOptions>(),
                    c.GetRequiredService<IHostSettings>()));

                // Register concrete implementations of relay workers
                services.AddSingleton<IQueueMonitoringApiRelayWorker<AuditMessage>, DeliverAuditRelayWorker>();
                services.AddSingleton<IQueueMonitoringApiRelayWorker<UsageMessage>, DeliverUsageRelayWorker>();
                services
                    .AddSingleton<IQueueMonitoringApiRelayWorker<ProvisioningMessage>,
                        DeliverProvisioningRelayWorker>();
                services.AddSingleton<IQueueMonitoringApiRelayWorker<EmailMessage>, SendEmailRelayWorker>();
                services.AddSingleton<IQueueMonitoringApiRelayWorker<SmsMessage>, SendSmsRelayWorker>();
                services
                    .AddSingleton<IMessageBusMonitoringApiRelayWorker<DomainEventingMessage>,
                        DeliverDomainEventingRelayWorker>();

                // Register generic delivery wrappers with the corresponding queue names
                services.AddSingleton<QueueDelivery<AuditMessage>>(sp =>
                    new QueueDelivery<AuditMessage>(
                        sp.GetRequiredService<IQueueMonitoringApiRelayWorker<AuditMessage>>(),
                        WorkerConstants.Queues.Audits));

                services.AddSingleton<QueueDelivery<UsageMessage>>(sp =>
                    new QueueDelivery<UsageMessage>(
                        sp.GetRequiredService<IQueueMonitoringApiRelayWorker<UsageMessage>>(),
                        WorkerConstants.Queues.Usages));

                services.AddSingleton<QueueDelivery<ProvisioningMessage>>(sp =>
                    new QueueDelivery<ProvisioningMessage>(
                        sp.GetRequiredService<IQueueMonitoringApiRelayWorker<ProvisioningMessage>>(),
                        WorkerConstants.Queues.Provisionings));

                services.AddSingleton<QueueDelivery<EmailMessage>>(sp =>
                    new QueueDelivery<EmailMessage>(
                        sp.GetRequiredService<IQueueMonitoringApiRelayWorker<EmailMessage>>(),
                        WorkerConstants.Queues.Emails));

                services.AddSingleton<QueueDelivery<SmsMessage>>(sp =>
                    new QueueDelivery<SmsMessage>(
                        sp.GetRequiredService<IQueueMonitoringApiRelayWorker<SmsMessage>>(),
                        WorkerConstants.Queues.Smses));

                // Register generic message bus wrapper for domain events
                services.AddSingleton<MessageBusDelivery<DomainEventingMessage>>(sp =>
                    new MessageBusDelivery<DomainEventingMessage>(
                        sp.GetRequiredService<IMessageBusMonitoringApiRelayWorker<DomainEventingMessage>>(),
                        WorkerConstants.MessageBuses.SubscriberHosts.ApiHost1,
                        WorkerConstants.MessageBuses.Topics.DomainEvents));

                // Register the MultiRelayWorker as a hosted service
                services.AddHostedService<MultiRelayWorker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();

        await host.RunAsync();
    }
}