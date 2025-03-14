using OnPremisesWorkerService.Core.Abstractions;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Infrastructure.Workers.Api;
using Application.Persistence.Shared.ReadModels;
using Common.Extensions;

namespace OnPremisesWorkerService.Infrastructure.MessageBus;

public class RabbitMqListenerService : BackgroundService
{
    private readonly IRabbitMqListenerServiceConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqListenerService> _logger;
    private IModel? _channel;

    public RabbitMqListenerService(
        IRabbitMqListenerServiceConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqListenerService> logger
    )
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_connection.IsConnected)
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                InitializeChannel(stoppingToken);

                if (_channel.NotExists())
                {
                    continue;
                }

                var consumerTasks = new List<Task>
                {
                    StartQueueConsumer<UsageMessage>(WorkerConstants.Queues.Audits, stoppingToken),
                    StartQueueConsumer<AuditMessage>(WorkerConstants.Queues.Usages, stoppingToken),
                    StartQueueConsumer<EmailMessage>(WorkerConstants.Queues.Emails, stoppingToken),
                    StartQueueConsumer<SmsMessage>(WorkerConstants.Queues.Smses, stoppingToken),
                    StartQueueConsumer<ProvisioningMessage>(WorkerConstants.Queues.Provisionings, stoppingToken),
                    StartSubscriberConsumer<DomainEventingMessage>(WorkerConstants.MessageBuses.Subscribers.ApiHost1,
                        "", stoppingToken)
                };

                await Task.WhenAll(consumerTasks);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in consumer execution");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private void InitializeChannel(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        _channel.BasicQos(0, 1, false);

        _channel.ExchangeDeclare(
            WorkerConstants.MessageBuses.Topics.DomainEvents,
            ExchangeType.Topic,
            durable: true
        );

        DeclareAndBindQueue(
            queueName: WorkerConstants.MessageBuses.Subscribers.ApiHost1.ToLower(),
            exchangeName: WorkerConstants.MessageBuses.Topics.DomainEvents,
            routingKey: "#"
        );

        DeclareAndBindQueue(WorkerConstants.Queues.Audits, null, "");
        DeclareAndBindQueue(WorkerConstants.Queues.Usages, null, "");
        DeclareAndBindQueue(WorkerConstants.Queues.Emails, null, "");
        DeclareAndBindQueue(WorkerConstants.Queues.Smses, null, "");
        DeclareAndBindQueue(WorkerConstants.Queues.Provisionings, null, "");
    }

    private void DeclareAndBindQueue(string queueName, string? exchangeName, string routingKey)
    {
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        if (exchangeName != null)
        {
            _channel.QueueBind(
                queue: queueName,
                exchange: exchangeName,
                routingKey: routingKey
            );
        }
    }

    private async Task StartQueueConsumer<TMessage>(string queueName, CancellationToken token)
        where TMessage : IQueuedMessage
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (object _, BasicDeliverEventArgs ea) =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IQueueMonitoringApiRelayWorker<TMessage>>();
                var message = JsonSerializer.Deserialize<TMessage>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (message != null)
                {
                    await handler.RelayMessageOrThrowAsync(message, token);
                }

                _channel?.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje en {QueueName}", queueName);
                _channel?.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        await Task.Delay(Timeout.Infinite, token);
    }

    private async Task StartSubscriberConsumer<TMessage>(
        string subscriberId,
        string routingKey,
        CancellationToken token
    )
        where TMessage : IQueuedMessage
    {
        var queueName = $"{WorkerConstants.MessageBuses.Topics.DomainEvents}_{subscriberId}";

        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(
            queue: queueName,
            exchange: WorkerConstants.MessageBuses.Topics.DomainEvents,
            routingKey: routingKey
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var worker = scope.ServiceProvider
                    .GetRequiredService<IMessageBusMonitoringApiRelayWorker<TMessage>>();
                var message = JsonSerializer.Deserialize<TMessage>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (message != null)
                {
                    await worker.RelayMessageOrThrowAsync(
                        WorkerConstants.MessageBuses.Subscribers.ApiHost1, message, token);
                }

                _channel?.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mensaje en {QueueName}", queueName);
                _channel?.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        await Task.Delay(Timeout.Infinite, token);
    }

    public override void Dispose()
    {
        _channel?.Close();
        base.Dispose();
    }
}