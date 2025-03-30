#if TESTINGONLY
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.External.Persistence.TestingOnly.ApplicationServices;

partial class LocalMachineJsonFileStore : IMessageBusStore, IMessageBusStoreTrigger
{
    private const string MessageBusStoreSubscriptionContainerName = "MessageBus/Subscriptions";
    private const string MessageBusStoreTopicContainerName = "MessageBus/Topics";

#if TESTINGONLY
    async Task<Result<long, Error>> IMessageBusStore.CountAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.InProcessInMemDataStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName),
            Resources.InProcessInMemDataStore_MissingSubscriptionName);

        var topicStore = EnsureContainer(GetTopicStoreContainerPath(topicName));
        if (topicStore.IsEmpty())
        {
            return 0;
        }

        var subscriptionsStore = EnsureContainer(GetSubscriptionStoreContainerPath(topicName));
        if (subscriptionsStore.IsEmpty())
        {
            return 0;
        }

        if (subscriptionsStore.NotExists(subscriptionName))
        {
            return 0;
        }

        var subscription = await LoadSubscriptionAsync(subscriptionsStore, subscriptionName, cancellationToken);
        if (subscription.TopicName.EqualsIgnoreCase(topicName))
        {
            var messageIds = topicStore
                .GetEntityIds()
                .Select(name => name.ToLong())
                .OrderBy(ticks => ticks)
                .ToList();
            var count = subscription.Current.HasValue
                ? messageIds.Count(ticks => ticks > subscription.Current.Value)
                : messageIds.Count();

            return count;
        }

        return 0;
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IMessageBusStore.DestroyAllAsync(string topicName, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.InProcessInMemDataStore_MissingTopicName);

        var topicStore = EnsureContainer(GetTopicStoreContainerPath(topicName));
        topicStore.Erase();

        var subscriptionsStore = EnsureContainer(GetSubscriptionStoreContainerPath(topicName));
        subscriptionsStore.Erase();

        return Task.FromResult(Result.Ok);
    }
#endif

#if TESTINGONLY
    public async Task<Result<bool, Error>> ReceiveSingleAsync(string topicName, string subscriptionName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.InProcessInMemDataStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter((string)nameof(subscriptionName),
            Resources.InProcessInMemDataStore_MissingSubscriptionName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        var topicStore = EnsureContainer(GetTopicStoreContainerPath(topicName));
        if (topicStore.IsEmpty())
        {
            return false;
        }

        var subscriptionsStore = EnsureContainer(GetSubscriptionStoreContainerPath(topicName));
        await EnsureSubscriptionExists();

        var subscription = await LoadSubscriptionAsync(subscriptionsStore, subscriptionName, cancellationToken);
        if (subscription.NotExists())
        {
            return false;
        }

        var messageIds = topicStore
            .GetEntityIds()
            .Select(name => name.ToLong())
            .OrderBy(ticks => ticks)
            .ToList();
        long? latestMessageId = subscription.Current.HasValue
            ? messageIds.FirstOrDefault(ticks => ticks > subscription.Current.Value)
            : messageIds.First();

        if (latestMessageId is null or 0)
        {
            //Note: no more messages for this subscription
            return false;
        }

        var latestMessage = await topicStore.ReadAsync(latestMessageId.ToString()!, cancellationToken);
        if (latestMessage.NotExists())
        {
            //Note: no more messages for this subscription
            return false;
        }

        var message = latestMessage["Message"].Value;
        try
        {
            var handled = await messageHandlerAsync(message, cancellationToken);
            if (handled.IsFailure)
            {
                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            return ex.ToError(ErrorCode.Unexpected);
        }

        //Note: update the subscription position to latest
        subscription.Current = latestMessageId;
        await SaveSubscriptionAsync(subscriptionsStore, subscription, cancellationToken);
        return true;

        async Task EnsureSubscriptionExists()
        {
            if (subscriptionsStore.NotExists(subscriptionName))
            {
                await SaveSubscriptionAsync(subscriptionsStore, new SubscriptionPosition(subscriptionName, topicName),
                    cancellationToken);
            }
        }
    }
#endif

    public async Task<Result<Error>> SendAsync(string topicName, string message, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.InProcessInMemDataStore_MissingTopicName);
        message.ThrowIfNotValuedParameter((string)nameof(message),
            Resources.InProcessInMemDataStore_MissingQueueMessage);

        // Note: queue message with timestamp
        var topicStore = EnsureContainer(GetTopicStoreContainerPath(topicName));
        var messageId = DateTime.UtcNow.Ticks.ToString();
        await topicStore.WriteAsync(messageId, new Dictionary<string, Optional<string>>
        {
            { "Message", message }
        }, cancellationToken);

        return Result.Ok;
    }

    public async Task<Result<Error>> SubscribeAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.InProcessInMemDataStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter((string)nameof(subscriptionName),
            Resources.InProcessInMemDataStore_MissingSubscriptionName);

        EnsureContainer(GetTopicStoreContainerPath(topicName));
        var subscriptionsStore = EnsureContainer(GetSubscriptionStoreContainerPath(topicName));
        if (subscriptionsStore.NotExists(subscriptionName))
        {
            await SaveSubscriptionAsync(subscriptionsStore, new SubscriptionPosition(subscriptionName, topicName),
                cancellationToken);
        }

        return Result.Ok;
    }

    public event MessageQueueUpdated? FireTopicMessage;

    private static async Task<SubscriptionPosition> LoadSubscriptionAsync(FileContainer subscriptionsStore,
        string subscriptionName, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionsStore.ReadAsync(subscriptionName, cancellationToken);
        var topicName = subscription[nameof(SubscriptionPosition.TopicName)].Value;
        long? current = subscription.ContainsKey(nameof(SubscriptionPosition.Current))
            ? subscription[nameof(SubscriptionPosition.Current)].Value.ToLong()
            : null;
        return new SubscriptionPosition(subscriptionName, topicName)
        {
            Current = current
        };
    }

    private static async Task SaveSubscriptionAsync(FileContainer subscriptionsStore, SubscriptionPosition subscription,
        CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, Optional<string>>
        {
            { nameof(SubscriptionPosition.TopicName), subscription.TopicName }
        };
        if (subscription.Current.HasValue)
        {
            properties[nameof(SubscriptionPosition.Current)] = subscription.Current.Value.ToString();
        }

        await subscriptionsStore.WriteAsync(subscription.SubscriptionName, properties, cancellationToken);
    }

    private void NotifyPendingBusTopicMessages()
    {
        var container = EnsureContainer(GetTopicStoreContainerPath());
        if (container.Containers().HasNone())
        {
            return;
        }

        if (FireTopicMessage.NotExists())
        {
            return;
        }

        foreach (var topic in container.Containers())
        {
            var messageCount = (int)topic.Count;
            if (messageCount > 0)
            {
                FireTopicMessage?.Invoke(this, new MessagesQueueUpdatedArgs(topic.Name, messageCount));
            }
        }
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool TryNotifyBusTopicMessage(string createdFilePath)
    {
        var container = EnsureContainer(GetTopicStoreContainerPath());
        if (container.Containers().HasNone())
        {
            return false;
        }

        var file = new FileInfo(createdFilePath);
        var topicsDirectory = file.Directory!.Parent!.FullName;
        if (topicsDirectory.HasNoValue())
        {
            return false;
        }

        if (!container.IsPath(topicsDirectory))
        {
            return false;
        }

        var topicName = file.Directory.Name;
        var topicStore = EnsureContainer(GetTopicStoreContainerPath(topicName));
        var messageCount = (int)topicStore.Count;
        if (messageCount > 0)
        {
            FireTopicMessage?.Invoke(this, new MessagesQueueUpdatedArgs(topicName, messageCount));
        }

        return true;
    }

    private static string GetTopicStoreContainerPath(string? containerName = null, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{MessageBusStoreTopicContainerName}/{containerName}/{entityId}";
        }

        if (containerName.HasValue())
        {
            return $"{MessageBusStoreTopicContainerName}/{containerName}";
        }

        return $"{MessageBusStoreTopicContainerName}";
    }

    private static string GetSubscriptionStoreContainerPath(string? containerName = null, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{MessageBusStoreSubscriptionContainerName}/{containerName}/{entityId}";
        }

        if (containerName.HasValue())
        {
            return $"{MessageBusStoreSubscriptionContainerName}/{containerName}";
        }

        return $"{MessageBusStoreSubscriptionContainerName}";
    }

    private class SubscriptionPosition
    {
        public SubscriptionPosition(string subscriptionName, string topicName)
        {
            SubscriptionName = subscriptionName;
            TopicName = topicName;
            Current = null;
        }

        public long? Current { get; set; }

        public string SubscriptionName { get; }

        public string TopicName { get; }
    }
}
#endif