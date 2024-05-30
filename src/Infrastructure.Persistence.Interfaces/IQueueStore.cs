using Common;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines read/write access of messages to and from a FIFO queue store,
///     that a single consumer consumes messages from.
///     Order is guaranteed for only one consumer.
///     (e.g. a cloud queue, a message broker, or a service bus)
/// </summary>
public interface IQueueStore
{
#if TESTINGONLY
    /// <summary>
    ///     Returns the count of messages on the queue
    /// </summary>
    Task<Result<long, Error>> CountAsync(string queueName, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    /// <summary>
    ///     Destroys all data on the queue
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(string queueName, CancellationToken cancellationToken);
#endif

    /// <summary>
    ///     Pops the next waiting message from the specified queue, and processes it with the provided handler.
    /// </summary>
    /// <returns>True when a message (exists) and is handled, False otherwise</returns>
    Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Pushes a new message onto the queue
    /// </summary>
    Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken);
}