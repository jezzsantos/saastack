using System.Text.Json;
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

    // Dictionary mapping routing keys to processing delegates for queue messages.
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _queueProcessors;
    // Dictionary mapping routing keys to processing delegates for message bus messages.
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _messageBusProcessors;

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

        // Build dictionary mapping routing keys (or queue names) to processing delegates.
        _queueProcessors =
            new Dictionary<string, Func<string, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    WorkerConstants.Queues.Audits, async (json, ct) =>
                    {
                        var msg = JsonSerializer.Deserialize<AuditMessage>(json);
                        if (msg != null)
                        {
                            await auditDelivery.ProcessMessageAsync(msg, ct);
                        }
                    }
                },
                {
                    WorkerConstants.Queues.Usages, async (json, ct) =>
                    {
                        var msg = JsonSerializer.Deserialize<UsageMessage>(json);
                        if (msg != null)
                        {
                            await usageDelivery.ProcessMessageAsync(msg, ct);
                        }
                    }
                },
                {
                    WorkerConstants.Queues.Provisionings, async (json, ct) =>
                    {
                        var msg = JsonSerializer.Deserialize<ProvisioningMessage>(json);
                        if (msg != null)
                        {
                            await provisioningDelivery.ProcessMessageAsync(msg, ct);
                        }
                    }
                },
                {
                    WorkerConstants.Queues.Emails, async (json, ct) =>
                    {
                        var msg = JsonSerializer.Deserialize<EmailMessage>(json);
                        if (msg != null)
                        {
                            await emailDelivery.ProcessMessageAsync(msg, ct);
                        }
                    }
                },
                {
                    WorkerConstants.Queues.Smses, async (json, ct) =>
                    {
                        var msg = JsonSerializer.Deserialize<SmsMessage>(json);
                        if (msg != null)
                        {
                            await smsDelivery.ProcessMessageAsync(msg, ct);
                        }
                    }
                }
            };

        // Build dictionary for message bus processors.
        _messageBusProcessors =
            new Dictionary<string, Func<string, CancellationToken, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    WorkerConstants.MessageBuses.Topics.DomainEvents, async (json, ct) =>
                    {
                        var msg = JsonSerializer.Deserialize<DomainEventingMessage>(json);
                        if (msg != null)
                        {
                            await domainEventDelivery.ProcessMessageAsync(msg, ct);
                        }
                    }
                }
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
            try
            {
                var routingKey = e.RoutingKey;
                var json = e.Message;

                _logger.LogInformation("Received message from RoutingKey {RoutingKey}: {Message}", routingKey, json);

                if (_queueProcessors.TryGetValue(routingKey, out var processor))
                {
                    await processor(json, stoppingToken);
                }
                else if (_messageBusProcessors.TryGetValue(routingKey, out var busProcessor))
                {
                    await busProcessor(json, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("No processor found for RoutingKey: {RoutingKey}", routingKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message.");
            }
        };

        await _messageBroker.StartAsync(stoppingToken);

        // Keep the worker running until cancellation is requested.
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
}