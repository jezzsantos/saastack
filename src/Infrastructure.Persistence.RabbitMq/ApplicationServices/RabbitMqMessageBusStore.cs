using System.Text;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.RabbitMq.Extensions;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Infrastructure.Persistence.RabbitMq.ApplicationServices
{
    [UsedImplicitly]
    public sealed class RabbitMqMessageBusStore : IMessageBusStore, IAsyncDisposable
    {
        private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
        private readonly RabbitMqStoreOptions _options;
        private readonly IRecorder _recorder;
        private readonly Dictionary<string, TopicExistence> _exchangeExistenceChecks = new();
        private IConnection? _connection;

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
        public async Task<Result<long, Error>> CountAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AnyStore_MissingTopicName);
            subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName), Resources.AnyStore_MissingSubscriptionName);

            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();

            EnsureConnected();
            using var channel = _connection!.CreateModel();
            try
            {
                var queueDeclareOk = channel.QueueDeclarePassive(sanitizedQueueName);
                return queueDeclareOk.MessageCount;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "Failed to count messages for queue: {Queue} in exchange: {Exchange}", sanitizedQueueName, sanitizedExchangeName);
                return 0;
            }
        }
#endif

#if TESTINGONLY
        public async Task<Result<Error>> DestroyAllAsync(string topicName, CancellationToken cancellationToken)
        {
            topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AnyStore_MissingTopicName);

            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();

            EnsureConnected();
            using var channel = _connection!.CreateModel();
            try
            {
                if (_exchangeExistenceChecks.TryGetValue(sanitizedExchangeName, out var topicExistence))
                {
                    foreach (var subscription in topicExistence.Subscriptions.Keys)
                    {
                        try
                        {
                            channel.QueueDelete(queue: subscription, ifUnused: false, ifEmpty: false);
                        }
                        catch (Exception ex)
                        {
                            _recorder.TraceError(null, ex,
                                "Failed to delete queue: {Queue} bound to exchange: {Exchange}",
                                subscription, sanitizedExchangeName);
                        }
                    }
                }
                channel.ExchangeDelete(exchange: sanitizedExchangeName, ifUnused: false);
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex,
                    "Failed to delete exchange and associated queues for: {Exchange}",
                    sanitizedExchangeName);
                return ex.ToError(ErrorCode.Unexpected);
            }

            _exchangeExistenceChecks.Remove(sanitizedExchangeName);
            await Task.CompletedTask;
            return Result.Ok;
        }
#endif

#if TESTINGONLY
        public async Task<Result<bool, Error>> ReceiveSingleAsync(string topicName, string subscriptionName,
            Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
            CancellationToken cancellationToken)
        {
            topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AnyStore_MissingTopicName);
            subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName), Resources.AnyStore_MissingSubscriptionName);
            ArgumentNullException.ThrowIfNull(messageHandlerAsync);

            EnsureConnected();
            using var channel = _connection!.CreateModel();
            var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();

            if (!IsSubscriptionExistenceCheckPerformed(topicName.SanitizeAndValidateTopicName(), sanitizedQueueName))
            {
                await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
            }

            RabbitMQ.Client.BasicGetResult? result = null;
            var startTime = DateTime.UtcNow;
            while (result == null && DateTime.UtcNow - startTime < ReceiveTimeout)
            {
                result = channel.BasicGet(sanitizedQueueName, autoAck: false);
                if (result == null)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            if (result == null)
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

        public async Task<Result<Error>> SendAsync(string topicName, string message, CancellationToken cancellationToken)
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
                _recorder.TraceError(null, ex, "Failed to send message: {Message} to exchange: {Exchange}", message, topicName);
                return ex.ToError(ErrorCode.Unexpected);
            }

            return Result.Ok;
        }

        public async Task<Result<Error>> SubscribeAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
            return Result.Ok;
        }

        private async Task<Result<BasicGetResult?, Error>> RetrieveNextMessageInternalAsync(string topicName,
            string subscriptionName, IModel channel, CancellationToken cancellationToken)
        {
            try
            {
                var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();
                var result = channel.BasicGet(sanitizedQueueName, autoAck: false);
                return result;
            }
            catch (Exception)
            {
                await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
                var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();
                var result = channel.BasicGet(sanitizedQueueName, autoAck: false);
                return result;
            }
        }

        private async Task<Result<Error>> SendMessageInternalAsync(string topicName, string message, CancellationToken cancellationToken)
        {
            try
            {
                EnsureConnected();
                using var channel = _connection!.CreateModel();
                var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
                if (!IsTopicExistenceCheckPerformed(sanitizedExchangeName))
                {
                    await CreateTopicAsync(topicName, cancellationToken);
                }
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: sanitizedExchangeName,
                                     routingKey: "",
                                     basicProperties: null,
                                     body: body);
                return Result.Ok;
            }
            catch (Exception)
            {
                await CreateTopicAsync(topicName, cancellationToken);
                using var channel = _connection!.CreateModel();
                var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: sanitizedExchangeName,
                                     routingKey: "",
                                     basicProperties: null,
                                     body: body);
                return Result.Ok;
            }
        }

        private async Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken)
        {
            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            EnsureConnected();
            using var channel = _connection!.CreateModel();
            try
            {
                channel.ExchangeDelete(sanitizedExchangeName, false);
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "Failed to delete exchange: {Exchange}", sanitizedExchangeName);
            }
            await Task.CompletedTask;
        }

        private void EnsureConnected()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port ?? 5672,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    VirtualHost = _options.VirtualHost
                };
                _connection = factory.CreateConnection();
            }
        }

        private IModel GetAdminChannel()
        {
            EnsureConnected();
            return _connection!.CreateModel();
        }

        private async Task<IModel> ConnectToTopicAsync(string topicName, CancellationToken cancellationToken)
        {
            EnsureConnected();
            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            if (!IsTopicExistenceCheckPerformed(sanitizedExchangeName))
            {
                await CreateTopicAsync(topicName, cancellationToken);
            }
            var channel = _connection!.CreateModel();
            return channel;
        }

        private async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken)
        {
            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            using var channel = GetAdminChannel();
            try
            {
                channel.ExchangeDeclare(exchange: sanitizedExchangeName,
                                          type: "topic",
                                          durable: true,
                                          autoDelete: false,
                                          arguments: null);
                IsTopicExistenceCheckPerformed(sanitizedExchangeName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "Failed to create exchange: {Exchange}", sanitizedExchangeName);
                throw;
            }
        }

        private async Task CreateSubscriptionAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            await CreateTopicAsync(topicName, cancellationToken);
            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();
            using var channel = GetAdminChannel();
            try
            {
                channel.QueueDeclare(queue: sanitizedQueueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                channel.QueueBind(queue: sanitizedQueueName,
                                  exchange: sanitizedExchangeName,
                                  routingKey: "#");
                IsSubscriptionExistenceCheckPerformed(sanitizedExchangeName, sanitizedQueueName);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "Failed to create queue binding: {Queue} to exchange: {Exchange}", sanitizedQueueName, sanitizedExchangeName);
                throw;
            }
        }

        private async Task<IModel> ConnectReceiverAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            EnsureConnected();
            var sanitizedExchangeName = topicName.SanitizeAndValidateTopicName();
            var sanitizedQueueName = subscriptionName.SanitizeAndValidateSubscriptionName();
            if (!IsSubscriptionExistenceCheckPerformed(sanitizedExchangeName, sanitizedQueueName))
            {
                await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
            }
            return _connection!.CreateModel();
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
                if (obj is not TopicExistence other)
                    return false;
                return string.Equals(_exchangeName, other._exchangeName, StringComparison.InvariantCultureIgnoreCase);
            }

            public override int GetHashCode() =>
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(_exchangeName);
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
                    return false;
                return string.Equals(_queueName, other._queueName, StringComparison.InvariantCultureIgnoreCase);
            }

            public override int GetHashCode() =>
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(_queueName);
        }

    }
}
