using System.Collections.Concurrent;
using Application.Interfaces;
using Application.Persistence.Shared.ReadModels;
using OnPremRelay.WorkerHost.Messaging.Delivery;
using OnPremRelay.WorkerHost.Messaging.Interfaces;

namespace OnPremRelay.WorkerHost.Workers;

/// <summary>
///     A hosted service that routes incoming messages from RabbitMQ to the appropriate delivery service
///     based on the routing key.
/// </summary>
public class MultiRelayWorker : BackgroundService
{
    private readonly IMessageBrokerService _messageBroker;
    private readonly ILogger<MultiRelayWorker> _logger;

    private readonly int _maxFailureCount = 5;
    private readonly ConcurrentDictionary<string, int> _failureCounts = new ConcurrentDictionary<string, int>();

    private readonly Dictionary<string, Func<string, string, CancellationToken, Task>> _queueProcessors;
    private readonly Dictionary<string, Func<string, string, CancellationToken, Task>> _messageBusProcessors;
    private readonly RabbitMqMetricsRegistry _metrics;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MultiRelayWorker" /> class.
    /// </summary>
    /// <param name="messageBroker">The message broker service.</param>
    /// <param name="auditDelivery">Delivery service for Audit messages.</param>
    /// <param name="usageDelivery">Delivery service for Usage messages.</param>
    /// <param name="provisioningDelivery">Delivery service for Provisioning messages.</param>
    /// <param name="emailDelivery">Delivery service for Email messages.</param>
    /// <param name="smsDelivery">Delivery service for SMS messages.</param>
    /// <param name="domainEventDelivery">Delivery service for Domain Event messages.</param>
    /// <param name="logger">The logger instance.</param>
    public MultiRelayWorker(
        IMessageBrokerService messageBroker,
        QueueDelivery<AuditMessage> auditDelivery,
        QueueDelivery<UsageMessage> usageDelivery,
        QueueDelivery<ProvisioningMessage> provisioningDelivery,
        QueueDelivery<EmailMessage> emailDelivery,
        QueueDelivery<SmsMessage> smsDelivery,
        MessageBusDelivery<DomainEventingMessage> domainEventDelivery,
        ILogger<MultiRelayWorker> logger)
    {
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _queueProcessors =
            new Dictionary<string, Func<string, string, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    WorkerConstants.Queues.Audits, async (json, routingKey, ct) =>
                        await auditDelivery.HandleDeliveryAsync<AuditMessage>(json, routingKey,
                            async (msg, token) => await auditDelivery.ProcessMessageAsync(msg, token),
                            _failureCounts.GetValueOrDefault(WorkerConstants.Queues.Audits), ct)
                },
                // {
                //     WorkerConstants.Queues.Usages, async (json, routingKey, ct) =>
                //         await usageDelivery.HandleDeliveryAsync<UsageMessage>(json, routingKey,
                //             async (msg, token) => await usageDelivery.ProcessMessageAsync(msg, token),
                //             _failureCounts.GetValueOrDefault(WorkerConstants.Queues.Audits), ct)
                // },
                // {
                //     WorkerConstants.Queues.Provisionings, async (json, routingKey, ct) =>
                //         await provisioningDelivery.HandleDeliveryAsync<ProvisioningMessage>(json, routingKey,
                //             async (msg, token) => await provisioningDelivery.ProcessMessageAsync(msg, token),
                //             _failureCounts.GetValueOrDefault(WorkerConstants.Queues.Audits), ct)
                // },
                // {
                //     WorkerConstants.Queues.Emails, async (json, routingKey, ct) =>
                //         await emailDelivery.HandleDeliveryAsync<EmailMessage>(json, routingKey,
                //             async (msg, token) => await emailDelivery.ProcessMessageAsync(msg, token),
                //             _failureCounts.GetValueOrDefault(WorkerConstants.Queues.Emails), ct)
                // },
                // {
                //     WorkerConstants.Queues.Smses, async (json, routingKey, ct) =>
                //         await smsDelivery.HandleDeliveryAsync<SmsMessage>(json, routingKey,
                //             async (msg, token) => await smsDelivery.ProcessMessageAsync(msg, token),
                //             _failureCounts.GetValueOrDefault(WorkerConstants.Queues.Smses), ct)
                // }
            };

        _messageBusProcessors =
            new Dictionary<string, Func<string, string, CancellationToken, Task>>(StringComparer
                .OrdinalIgnoreCase)
            {
                // {
                //     WorkerConstants.MessageBuses.Topics.DomainEvents, async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-UserProfiles-EndUser".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-UserProfiles-Image".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-EndUsers-Organization".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-EndUsers-Subscription".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-Organizations-EndUser".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-Organizations-Image".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-Organizations-Subscription".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // },
                // {
                //     "ApiHost1-Subscriptions-Organization".ToLower(), async (json, subscriptionName, ct) =>
                //         await domainEventDelivery.HandleDeliveryAsync<DomainEventingMessage>(json, subscriptionName,
                //             async (msg, token) =>
                //                 await domainEventDelivery.ProcessMessageAsync(msg, subscriptionName, token),
                //             _failureCounts.GetValueOrDefault(subscriptionName), ct)
                // }
            };
    }

    /// <summary>
    ///     Executes the hosted service by subscribing to message broker events and routing messages.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A Task representing the background operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MultiRelayWorker started.");

        _messageBroker.MessageReceived += async (_, e) =>
        {
            var routingKey = e.RoutingKey;
            try
            {
                var json = e.Message;
                _logger.LogInformation("Received message from RoutingKey [{RoutingKey}]", routingKey.ToUpper());

                if (_queueProcessors.TryGetValue(routingKey, out var processor))
                {
                    await processor(json, routingKey, stoppingToken);
                    ResetFailureCount(routingKey);
                }
                else if (_messageBusProcessors.TryGetValue(routingKey, out var busProcessor))
                {
                    await busProcessor(json, routingKey, stoppingToken);
                    ResetFailureCount(routingKey);
                }
                else
                {
                    _logger.LogWarning("No se encontró un processor para la RoutingKey: {RoutingKey}", routingKey);
                }
            }
            catch (Exception ex)
            {
                int currentFailureCount = IncrementFailureCount(routingKey);
                _logger.LogError(
                    "Error en el procesamiento del mensaje para RoutingKey {RoutingKey}. Fallos consecutivos: {FailureCount} => {msg}",
                    routingKey, currentFailureCount, ex.Message);

                if (currentFailureCount >= _maxFailureCount)
                {
                    _logger.LogError(
                        "Circuit breaker activado para la RoutingKey {RoutingKey} tras {FailureCount} errores. Se recomienda intervenir manualmente o pausar el procesamiento de esta cola.",
                        routingKey, currentFailureCount);
                }
            }
        };

        await _messageBroker.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    /// <summary>
    ///     Stops the hosted service gracefully.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Task representing the asynchronous stop operation.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MultiRelayWorker is stopping.");
        await _messageBroker.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private int IncrementFailureCount(string routingKey)
    {
        return _failureCounts.AddOrUpdate(routingKey, 1, (_, current) => current + 1);
    }

    private void ResetFailureCount(string routingKey)
    {
        _failureCounts.AddOrUpdate(routingKey, 0, (_, __) => 0);
    }
}