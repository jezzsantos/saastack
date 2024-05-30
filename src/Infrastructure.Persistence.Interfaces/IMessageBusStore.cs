using Common;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines read/write access of messages to and from the FIFO topics of a message bus store,
///     that multiple subscriptions can consume messages from.
///     (e.g. a message broker, or a service bus)
///     Order is guaranteed for each subscription.
///     A subscription is only expected to receive a message if the subscription exists at the time the message is sent.
/// </summary>
public interface IMessageBusStore
{
#if TESTINGONLY
    /// <summary>
    ///     Returns the count of messages published for the subscription
    /// </summary>
    Task<Result<long, Error>> CountAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    /// <summary>
    ///     Destroys all data for the topic
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(string topicName, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    /// <summary>
    ///     Recives the next waiting message from the specified topic and subscription, and processes it with the provided
    ///     handler.
    /// </summary>
    /// <returns>True when a message (exists) and is handled, False otherwise</returns>
    Task<Result<bool, Error>> ReceiveSingleAsync(string topicName, string subscriptionName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken);
#endif

    /// <summary>
    ///     Sends a new message to the topic
    /// </summary>
    Task<Result<Error>> SendAsync(string topicName, string message, CancellationToken cancellationToken);

    /// <summary>
    ///     Registers a subscription to a topic, with the specified name
    /// </summary>
    Task<Result<Error>> SubscribeAsync(string topicName, string subscriptionName, CancellationToken cancellationToken);
}