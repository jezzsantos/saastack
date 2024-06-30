using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Infrastructure.Persistence.Azure.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Persistence.Azure.ApplicationServices;

/// <summary>
///     Provides a message bus store for Azure Service Bus.
///     Note: one the interesting characteristics of Azure Service Bus is that until a subscription is created, no
///     existing messages pushed to the topic will appear for the subscription.
///     Which means that the subscriptions need to be in place before the messages are sent to the topic, otherwise
///     they will be unrecoverable by the subscription when it is created.
/// </summary>
[UsedImplicitly]
public sealed class AzureServiceBusStore : IMessageBusStore, IAsyncDisposable
{
    private const string ConnectionStringSettingName =
        "ApplicationServices:Persistence:AzureServiceBus:ConnectionString";
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
    private readonly string _connectionString;
    private readonly IRecorder _recorder;
    private readonly Dictionary<string, TopicExistence> _topicExistenceChecks = new();
    private ServiceBusClient? _client;

    public static AzureServiceBusStore Create(IRecorder recorder, IConfigurationSettings settings)
    {
        var connection = settings.GetString(ConnectionStringSettingName);

        return new AzureServiceBusStore(recorder, connection);
    }

    private AzureServiceBusStore(IRecorder recorder, string connectionString)
    {
        _recorder = recorder;
        _connectionString = connectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.Exists())
        {
            await _client.DisposeAsync();
        }
    }

#if TESTINGONLY
    public async Task<Result<long, Error>> CountAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName), Resources.AzureServiceBusStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter((string)nameof(subscriptionName),
            Resources.AzureServiceBusStore_MissingSubscriptionName);

        var admin = new ServiceBusAdministrationClient(_connectionString);
        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();
        var sanitizedSubscriptionName = subscriptionName.SanitizeAndValidateSubscriptionName();

        if (!await admin.TopicExistsAsync(sanitizedTopicName, cancellationToken))
        {
            return 0;
        }

        var properties =
            await admin.GetSubscriptionRuntimePropertiesAsync(sanitizedTopicName, sanitizedSubscriptionName,
                cancellationToken);
        return properties.Exists()
            ? properties.Value.ActiveMessageCount
            : 0;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(string topicName, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.AzureServiceBusStore_MissingTopicName);

        // NOTE: deleting the entire topic may take far too long (this method is only tenable in testing)
        await DeleteTopicAsync(topicName, cancellationToken);

        _topicExistenceChecks.Remove(topicName);

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<bool, Error>> ReceiveSingleAsync(string topicName, string subscriptionName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName), Resources.AzureServiceBusStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter((string)nameof(subscriptionName),
            Resources.AzureServiceBusStore_MissingSubscriptionName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        await using var receiver = await ConnectReceiverAsync(topicName, subscriptionName, cancellationToken);

        Result<ServiceBusReceivedMessage?, Error> received;
        try
        {
            received = await RetrieveNextMessageInternalAsync(topicName, subscriptionName, receiver, cancellationToken);
            if (received.IsFailure)
            {
                return received.Error;
            }

            if (!received.HasValue
                || received.Value.NotExists())
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to receive message from topic: {Topic} for subscription: {Subscription}",
                topicName, subscriptionName);
            return ex.ToError(ErrorCode.Unexpected);
        }

        var topicMessage = received.Value!;
        try
        {
            var messageAsJson = topicMessage.Body.ToString();
            var handled = await messageHandlerAsync(messageAsJson, cancellationToken);
            if (handled.IsFailure)
            {
                await receiver.AbandonMessageAsync(topicMessage, null, cancellationToken);

                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            await receiver.AbandonMessageAsync(topicMessage, null, cancellationToken);

            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to handle last message: {MessageId} from topic: {Topic} for subscription: {Subscription}",
                topicMessage.MessageId, topicName, subscriptionName);
            return ex.ToError(ErrorCode.Unexpected);
        }

        await receiver.CompleteMessageAsync(topicMessage, cancellationToken);

        return true;
    }
#endif

    public async Task<Result<Error>> SendAsync(string topicName, string message, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName), Resources.AzureServiceBusStore_MissingTopicName);
        message.ThrowIfNotValuedParameter((string)nameof(message), Resources.AzureServiceBusStore_MissingSentMessage);

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
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to send message: {Message} to the topic: {Topic}", message, topicName);
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

    private async Task<Result<ServiceBusReceivedMessage?, Error>> RetrieveNextMessageInternalAsync(string topicName,
        string subscriptionName, ServiceBusReceiver receiver, CancellationToken cancellationToken)
    {
        var command = async () => await receiver.ReceiveMessageAsync(ReceiveTimeout, cancellationToken);

        try
        {
            return await command();
        }
        catch (ServiceBusException ex)
        {
            if (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
                return await command();
            }
        }

        return null;
    }

    private async Task<Result<Error>> SendMessageInternalAsync(string topicName, string message,
        CancellationToken cancellationToken)
    {
        var command = async () =>
        {
            await using var sender = await ConnectToTopicAsync(topicName, cancellationToken);

            using var batch = await sender.CreateMessageBatchAsync(cancellationToken);
            if (!batch.TryAddMessage(new ServiceBusMessage(message)))
            {
                return Error.RuleViolation(Resources.AzureServiceBusStore_MessageTooLarge);
            }

            await sender.SendMessagesAsync(batch, cancellationToken);

            return Result.Ok;
        };

        try
        {
            return await command();
        }
        catch (ServiceBusException ex)
        {
            if (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                await CreateTopicAsync(topicName, cancellationToken);
                return await command();
            }
        }

        return Result.Ok;
    }

    private async Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();
        var admin = new ServiceBusAdministrationClient(_connectionString);
        if (await admin.TopicExistsAsync(sanitizedTopicName, cancellationToken))
        {
            await admin.DeleteTopicAsync(sanitizedTopicName, cancellationToken);
        }
    }

    private void EnsureConnected()
    {
        if (_client.NotExists())
        {
            _client = new ServiceBusClient(_connectionString, new ServiceBusClientOptions());
        }
    }

    private async Task<ServiceBusSender> ConnectToTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        EnsureConnected();
        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();
        if (!IsTopicExistenceCheckPerformed(sanitizedTopicName))
        {
            await CreateTopicAsync(topicName, cancellationToken);
        }

        return _client!.CreateSender(sanitizedTopicName);
    }

    private async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();

        var admin = new ServiceBusAdministrationClient(_connectionString);
        if (!await admin.TopicExistsAsync(sanitizedTopicName, cancellationToken))
        {
            await admin.CreateTopicAsync(new CreateTopicOptions(sanitizedTopicName)
            {
                EnablePartitioning = false,
                SupportOrdering = true
            }, cancellationToken);
        }
    }

    private async Task CreateSubscriptionAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        await CreateTopicAsync(topicName, cancellationToken);

        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();
        var sanitizedSubscriptionName = subscriptionName.SanitizeAndValidateSubscriptionName();

        var admin = new ServiceBusAdministrationClient(_connectionString);
        if (!await admin.SubscriptionExistsAsync(sanitizedTopicName, sanitizedSubscriptionName, cancellationToken))
        {
            await admin.CreateSubscriptionAsync(
                new CreateSubscriptionOptions(sanitizedTopicName, sanitizedSubscriptionName), cancellationToken);
        }
    }

    private async Task<ServiceBusReceiver> ConnectReceiverAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        EnsureConnected();
        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();
        var sanitizedSubscriptionName = subscriptionName.SanitizeAndValidateSubscriptionName();
        if (!IsSubscriptionExistenceCheckPerformed(sanitizedTopicName, sanitizedSubscriptionName))
        {
            await CreateSubscriptionAsync(sanitizedTopicName, sanitizedSubscriptionName, cancellationToken);
        }

        return _client!.CreateReceiver(sanitizedTopicName, sanitizedSubscriptionName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock, // we want to manually complete messages
            PrefetchCount = 1 // we only want one (and only one) message at a time
        });
    }

    private bool IsTopicExistenceCheckPerformed(string topicName)
    {
        _topicExistenceChecks.TryAdd(topicName, new TopicExistence(topicName));
        if (_topicExistenceChecks[topicName].Exists)
        {
            return true;
        }

        _topicExistenceChecks[topicName].Exists = true;

        return false;
    }

    private bool IsSubscriptionExistenceCheckPerformed(string topicName, string subscriptionName)
    {
        if (!IsTopicExistenceCheckPerformed(topicName))
        {
            return false;
        }

        var topicExistence = _topicExistenceChecks[topicName];

        topicExistence.Subscriptions.TryAdd(subscriptionName, new SubscriptionExistence(subscriptionName));
        if (topicExistence.Subscriptions[subscriptionName].Exists)
        {
            return true;
        }

        topicExistence.Subscriptions[subscriptionName].Exists = true;

        return false;
    }

    private sealed class TopicExistence
    {
        private readonly string _topicName;

        public TopicExistence(string topicName)
        {
            _topicName = topicName;
            Exists = false;
            Subscriptions = new Dictionary<string, SubscriptionExistence>();
        }

        public bool Exists { get; set; }

        public Dictionary<string, SubscriptionExistence> Subscriptions { get; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((TopicExistence)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_topicName);
        }

        private bool Equals(TopicExistence other)
        {
            return string.Equals(_topicName, other._topicName, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    private sealed class SubscriptionExistence
    {
        private readonly string _subscriptionName;

        public SubscriptionExistence(string subscriptionName)
        {
            _subscriptionName = subscriptionName;
            Exists = false;
        }

        public bool Exists { get; set; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((SubscriptionExistence)obj);
        }

        public override int GetHashCode()
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(_subscriptionName);
        }

        private bool Equals(SubscriptionExistence other)
        {
            return string.Equals(_subscriptionName, other._subscriptionName,
                StringComparison.InvariantCultureIgnoreCase);
        }
    }
}