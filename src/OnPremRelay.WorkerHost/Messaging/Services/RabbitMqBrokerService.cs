using System.Text;
using Application.Interfaces;
using OnPremRelay.WorkerHost.Messaging.Delivery;
using OnPremRelay.WorkerHost.Messaging.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OnPremRelay.WorkerHost.Messaging.Services;

/// <summary>
///     RabbitMQ broker service that subscribes to multiple queues and raises events
///     with the source (queue name) and message content.
/// </summary>
public class RabbitMqBrokerService : IMessageBrokerService
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<RabbitMqBrokerService> _logger;
    private IModel _channel;
    // List of queue names to subscribe.
    private readonly IEnumerable<string> _queueNames = new List<string>
    {
        WorkerConstants.Queues.Audits,
        WorkerConstants.Queues.Usages,
        WorkerConstants.Queues.Provisionings,
        WorkerConstants.Queues.Emails,
        WorkerConstants.Queues.Smses,

        "ApiHost1-UserProfiles-EndUser".ToLower(),
        "ApiHost1-UserProfiles-Image".ToLower(),
        "ApiHost1-EndUsers-Organization".ToLower(),
        "ApiHost1-EndUsers-Subscription".ToLower(),
        "ApiHost1-Organizations-EndUser".ToLower(),
        "ApiHost1-Organizations-Image".ToLower(),
        "ApiHost1-Organizations-Subscription".ToLower(),
        "ApiHost1-Subscriptions-Organization".ToLower(),
    };

    /// <summary>
    ///     Event raised when a message is received, including the source (queue name) and message content.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs> MessageReceived;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RabbitMqBrokerService" /> class.
    /// </summary>
    /// <param name="connection">The RabbitMQ connection abstraction.</param>
    /// <param name="logger">The logger instance.</param>
    public RabbitMqBrokerService(IRabbitMqConnection connection, ILogger<RabbitMqBrokerService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    ///     Starts the broker service by declaring all required queues and subscribing to each.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting RabbitMQ Broker Service...");

        _channel = _connection.CreateModel();

        // Loop through each queue and set up a consumer.
        foreach (var queueName in _queueNames)
        {
            // Declare the queue (if it doesn't exist, it will be created).
            _channel.QueueDeclare(queueName, true, false, false, null);

            // Create an asynchronous consumer.
            var consumer = new AsyncEventingBasicConsumer(_channel);
            // Capture the current queue name for use in the lambda.
            var currentQueue = queueName;
            consumer.Received += async (_, ea) =>
            {
                try
                {
                    // Use the current queue name as the source.
                    var source = currentQueue;
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("Received message from queue [{QueueName}]", source.ToUpper());

                    // Se eleva el evento.
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(currentQueue, message));

                    // Solo si no hay error se confirman los mensajes.
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando mensaje de la cola {QueueName}. Se reintentará.",
                        currentQueue);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };
            
            _channel.BasicConsume(currentQueue, false, consumer);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Stops the broker service by closing the channel.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ Broker Service...");
        _channel.Close();
        return Task.CompletedTask;
    }
}