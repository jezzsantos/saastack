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
        WorkerConstants.Queues.Smses
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
                    _logger.LogInformation("Received message from queue {QueueName}: {Message}", source, message);

                    // Raise the event with the queue name and message.
                    MessageReceived(this, new MessageReceivedEventArgs(source, message));

                    // Acknowledge the message.
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}.", currentQueue);
                }
                // return Task.CompletedTask;
            };

            // Start consuming messages from the current queue.
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