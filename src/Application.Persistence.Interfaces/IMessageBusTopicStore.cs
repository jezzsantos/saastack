using Common;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for reading and writing a message to a topic on a message bus.
///     A bus topic being different from a <see cref="IMessageQueueStore" />.
/// </summary>
public interface IMessageBusTopicStore<TMessage>
    where TMessage : IQueuedMessage, new()
{
#if TESTINGONLY
    /// <summary>
    ///     Returns the total count of messages in the bus for the specified subscription
    /// </summary>
    Task<Result<long, Error>> CountAsync(string subscriptionName, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    /// <summary>
    ///     Permanently destroys all messages in the store
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    /// <summary>
    ///     Fetches the first message on the top of the topic for the specified subscription
    ///     and executes the <see cref="onMessageReceivedAsync" /> handler with it.
    /// </summary>
    Task<Result<bool, Error>> ReceiveSingleAsync(string subscriptionName,
        Func<TMessage, CancellationToken, Task<Result<Error>>> onMessageReceivedAsync,
        CancellationToken cancellationToken);
#endif

    /// <summary>
    ///     Adds a new message to the topic
    /// </summary>
    Task<Result<TMessage, Error>> SendAsync(ICallContext call, TMessage message, CancellationToken cancellationToken);
}