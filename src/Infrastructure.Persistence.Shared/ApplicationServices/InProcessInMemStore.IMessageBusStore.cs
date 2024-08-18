#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

partial class InProcessInMemStore : IMessageBusStore, IMessageBusStoreTrigger
{
    private readonly Dictionary<string, SubscriptionPosition> _subscriptions = new();
    private readonly Dictionary<string, Dictionary<long, HydrationProperties>> _topics = new();

#if TESTINGONLY
    Task<Result<long, Error>> IMessageBusStore.CountAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.InProcessInMemDataStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName),
            Resources.InProcessInMemDataStore_MissingSubscriptionName);

        if (!_topics.TryGetValue(topicName, out var _))
        {
            return Task.FromResult<Result<long, Error>>(0);
        }

        if (!_subscriptions.TryGetValue(subscriptionName, out var subscription))
        {
            return Task.FromResult<Result<long, Error>>(0);
        }

        var messages = _topics[topicName];
        var count = GetRecentMessages(messages, subscription.Current).Count;

        return Task.FromResult<Result<long, Error>>(count);
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IMessageBusStore.DestroyAllAsync(string topicName, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.InProcessInMemDataStore_MissingTopicName);

        if (_topics.ContainsKey(topicName))
        {
            _topics.Remove(topicName);
        }

        foreach (var subscription in _subscriptions)
        {
            if (subscription.Value.TopicName == topicName)
            {
                _subscriptions.Remove(subscription.Key);
            }
        }

        return Task.FromResult(Result.Ok);
    }
#endif

#if TESTINGONLY
    public async Task<Result<bool, Error>> ReceiveSingleAsync(string topicName, string subscriptionName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.InProcessInMemDataStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName),
            Resources.InProcessInMemDataStore_MissingSubscriptionName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        if (!_topics.TryGetValue(topicName, out _))
        {
            return false;
        }

        var messages = _topics[topicName];
        if (messages.HasNone())
        {
            return false;
        }

        if (!_subscriptions.TryGetValue(subscriptionName, out _))
        {
            _subscriptions.Add(subscriptionName, new SubscriptionPosition(topicName));
            return false;
        }

        var subscription = _subscriptions[subscriptionName];
        var latestMessage = GetRecentMessages(messages, subscription.Current).FirstOrDefault();
        if (latestMessage.Value.NotExists())
        {
            //Note: no more messages for this subscription
            return false;
        }

        var message = latestMessage.Value["Message"].ToString();
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
        subscription.Current = latestMessage.Key;
        return true;
    }
#endif

    public Task<Result<Error>> SendAsync(string topicName, string message, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.InProcessInMemDataStore_MissingTopicName);
        message.ThrowIfNotValuedParameter(nameof(message), Resources.InProcessInMemDataStore_MissingQueueMessage);

        if (!_topics.TryGetValue(topicName, out _))
        {
            _topics.Add(topicName, new Dictionary<long, HydrationProperties>());
        }

        var messages = _topics[topicName];

        // Note: queue message with timestamp
        var messageId = DateTime.UtcNow.Ticks;
        messages.Add(messageId, new HydrationProperties
        {
            { "Message", message }
        });

        FireTopicMessage?.Invoke(this, new MessagesQueueUpdatedArgs(topicName, messages.Count));

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> SubscribeAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName), Resources.InProcessInMemDataStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter(nameof(subscriptionName),
            Resources.InProcessInMemDataStore_MissingSubscriptionName);

        if (!_topics.TryGetValue(topicName, out _))
        {
            _topics.Add(topicName, new Dictionary<long, HydrationProperties>());
        }

        if (!_subscriptions.TryGetValue(subscriptionName, out _))
        {
            _subscriptions.Add(subscriptionName, new SubscriptionPosition(topicName));
        }

        return Task.FromResult(Result.Ok);
    }

    public event MessageQueueUpdated? FireTopicMessage;

    private static List<KeyValuePair<long, HydrationProperties>> GetRecentMessages(
        Dictionary<long, HydrationProperties> messages,
        long? current)
    {
        var ordered = messages.OrderBy(msg => msg.Key).ToList();
        return current.HasValue
            ? ordered.Where(msg => msg.Key > current.Value).ToList()
            : [ordered.First()];
    }

    private void NotifyPendingBusTopicMessages()
    {
        if (_topics.HasNone() || FireTopicMessage.NotExists())
        {
            return;
        }

        foreach (var (topicName, messages) in _topics)
        {
            var messageCount = messages.Count;
            if (messageCount > 0)
            {
                FireTopicMessage?.Invoke(this, new MessagesQueueUpdatedArgs(topicName, messageCount));
            }
        }
    }

    private class SubscriptionPosition
    {
        public SubscriptionPosition(string topicName)
        {
            TopicName = topicName;
            Current = null;
        }

        public long? Current { get; set; }

        public string TopicName { get; }
    }
}

#endif