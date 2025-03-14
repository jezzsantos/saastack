using System.Text;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.OnPremises.Extensions;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Infrastructure.Persistence.OnPremises.ApplicationServices;

[UsedImplicitly]
public sealed class RabbitMqMessageBusStore : IMessageBusStore, IAsyncDisposable
{
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
    private readonly RabbitMqStoreOptions _options;
    private readonly IRecorder _recorder;
    private readonly Dictionary<string, TopicExistence> _exchangeExistenceChecks = new();
    private IConnection? _connection;
    private const int DefaultRabbitMqPort = 5672;
    private readonly object _connectionLock = new();

    public static RabbitMqMessageBusStore Create(IRecorder recorder, RabbitMqStoreOptions options)
    {
        return new RabbitMqMessageBusStore(recorder, options);
    }

    private RabbitMqMessageBusStore(IRecorder recorder, RabbitMqStoreOptions options)
    {
        _recorder = recorder;
        _options = options;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null && _connection.IsOpen)
        {
            _connection.Close();
            _connection.Dispose();
        }

        await ValueTask.CompletedTask;
    }

#if TESTINGONLY
    public async Task<Result<long, Error>> CountAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AnyStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName),
            Resources.AnyStore_MissingSubscriptionName);

        var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
        var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();

        EnsureConnection();
        using var channel = _connection!.CreateModel();
        try
        {
            var queueDeclareOk = channel.QueueDeclarePassive(sanitizedQueueName);
            return queueDeclareOk.MessageCount;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "Failed to count messages for queue: {Queue} in exchange: {Exchange}",
                sanitizedQueueName, sanitizedExchangeName);
            return 0;
        }
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(string topicName, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AnyStore_MissingTopicName);

        var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();

        EnsureConnection();

        await DeleteTopicAsync(topicName, cancellationToken);

        _exchangeExistenceChecks.Remove(sanitizedExchangeName);

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<bool, Error>> ReceiveSingleAsync(string topicName, string subscriptionName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AnyStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName),
            Resources.AnyStore_MissingSubscriptionName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        EnsureConnection();
        using var channel = _connection!.CreateModel();
        var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();

        if (!IsSubscriptionExistenceCheckPerformed(topicName.SanitizeAndValidateTopicName(), sanitizedQueueName))
        {
            await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
        }

        BasicGetResult? result = null;
        var startTime = DateTime.UtcNow;
        while (result.NotExists() && DateTime.UtcNow - startTime < ReceiveTimeout)
        {
            result = channel.BasicGet(sanitizedQueueName, false);
            if (result.NotExists())
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        if (result.NotExists())
        {
            return false;
        }

        var body = Encoding.UTF8.GetString(result.Body.ToArray());
        try
        {
            var handled = await messageHandlerAsync(body, cancellationToken);
            if (handled.IsFailure)
            {
                channel.BasicNack(result.DeliveryTag, false, true);
                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            channel.BasicNack(result.DeliveryTag, false, true);
            _recorder.TraceError(null, ex,
                "Failed to handle message with DeliveryTag: {DeliveryTag} from queue: {Queue} in exchange: {Exchange}",
                result.DeliveryTag, sanitizedQueueName, topicName);
            return ex.ToError(ErrorCode.Unexpected);
        }

        channel.BasicAck(result.DeliveryTag, false);
        return true;
    }
#endif

    public async Task<Result<Error>> SendAsync(string topicName, string message,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AnyStore_MissingTopicName);
        message.ThrowIfNotValuedParameter(nameof(message), Resources.AnyStore_MissingSentMessage);

        try
        {
            var sent = await SendMessageInternalAsync(topicName, message, cancellationToken);
            if (sent.IsFailure)
            {
                return sent.Error;
            }
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "Failed to send message: {Message} to exchange: {Exchange}", message,
                topicName);
            return ex.ToError(ErrorCode.Unexpected);
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> SubscribeAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
        return Result.Ok;
    }

    private async Task<Result<Error>> SendMessageInternalAsync(string topicName, string message,
        CancellationToken cancellationToken)
    {
        try
        {
            EnsureConnection();
            using var channel = _connection!.CreateModel();
            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            if (!IsTopicExistenceCheckPerformed(sanitizedExchangeName))
            {
                await CreateTopicAsync(topicName, cancellationToken);
            }

            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(sanitizedExchangeName,
                "",
                null,
                body);
            return Result.Ok;
        }
        catch (Exception)
        {
            await CreateTopicAsync(topicName, cancellationToken);
            using var channel = _connection!.CreateModel();
            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(sanitizedExchangeName,
                "",
                null,
                body);
            return Result.Ok;
        }
    }

    private async Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;

        var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
        EnsureConnection();
        cancellationToken.ThrowIfCancellationRequested();
        
        using var channel = _connection!.CreateModel();
        try
        {
            if (_exchangeExistenceChecks.TryGetValue(sanitizedExchangeName, out var topicExistence))
            {
                foreach (var subscription in topicExistence.Subscriptions.Keys)
                {
                    try
                    {
                        channel.ExchangeDelete(sanitizedExchangeName, false);
                        channel.QueueDelete(subscription, false, false);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (Exception ex)
                    {
                        _recorder.TraceError(null, ex,
                            "Failed to delete queue: {Queue} bound to exchange: {Exchange}",
                            subscription, sanitizedExchangeName);
                    }
                }
            }

            channel.ExchangeDelete(sanitizedExchangeName, false);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex,
                "Failed to delete exchange and associated queues for: {Exchange}",
                sanitizedExchangeName);
        }
    }

    private void EnsureConnection()
    {
        lock (_connectionLock)
        {
            if (_connection.Exists() && _connection.IsOpen)
            {
                return;
            }

            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port ?? DefaultRabbitMqPort,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = "RabbitMqMessageBusStore",
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
        }
    }

    private IModel GetAdminChannel()
    {
        EnsureConnection();
        return _connection!.CreateModel();
    }

    private async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
        using var channel = GetAdminChannel();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            channel.ExchangeDeclare(sanitizedExchangeName,
                "topic",
                true,
                false,
                null);
            IsTopicExistenceCheckPerformed(sanitizedExchangeName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "Failed to create exchange: {Exchange}", sanitizedExchangeName);
            throw;
        }
    }

    private async Task CreateSubscriptionAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        await CreateTopicAsync(topicName, cancellationToken);
        var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
        var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();
        using var channel = GetAdminChannel();
        try
        {
            channel.QueueDeclare(sanitizedQueueName,
                true,
                false,
                false,
                null);
            channel.QueueBind(sanitizedQueueName,
                sanitizedExchangeName,
                "#");
            IsSubscriptionExistenceCheckPerformed(sanitizedExchangeName, sanitizedQueueName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "Failed to create queue binding: {Queue} to exchange: {Exchange}",
                sanitizedQueueName, sanitizedExchangeName);
            throw;
        }
    }

    private bool IsTopicExistenceCheckPerformed(string exchangeName)
    {
        if (!_exchangeExistenceChecks.TryGetValue(exchangeName, out var existence))
        {
            existence = new TopicExistence(exchangeName);
            _exchangeExistenceChecks.Add(exchangeName, existence);
        }

        if (existence.Exists)
        {
            return true;
        }

        existence.Exists = true;
        return false;
    }

    private bool IsSubscriptionExistenceCheckPerformed(string exchangeName, string queueName)
    {
        if (!IsTopicExistenceCheckPerformed(exchangeName))
        {
            return false;
        }

        var existence = _exchangeExistenceChecks[exchangeName];
        if (!existence.Subscriptions.TryGetValue(queueName, out var subExistence))
        {
            subExistence = new SubscriptionExistence(queueName);
            existence.Subscriptions.Add(queueName, subExistence);
        }

        if (subExistence.Exists)
        {
            return true;
        }

        subExistence.Exists = true;
        return false;
    }

    private sealed class TopicExistence
    {
        private readonly string _exchangeName;

        public TopicExistence(string exchangeName)
        {
            _exchangeName = exchangeName;
            Exists = false;
            Subscriptions = new Dictionary<string, SubscriptionExistence>();
        }

        public bool Exists { get; set; }

        public Dictionary<string, SubscriptionExistence> Subscriptions { get; }

        public override bool Equals(object? obj)
        {
            return obj is TopicExistence other && string.Equals(_exchangeName, other._exchangeName,
                StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_exchangeName);
        }
    }

    private sealed class SubscriptionExistence
    {
        private readonly string _queueName;

        public SubscriptionExistence(string queueName)
        {
            _queueName = queueName;
            Exists = false;
        }

        public bool Exists { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not SubscriptionExistence other)
            {
                return false;
            }

            return string.Equals(_queueName, other._queueName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_queueName);
        }
    }
}