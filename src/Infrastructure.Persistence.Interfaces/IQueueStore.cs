using Common;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines read/write access of messages to and from a queue store
///     (e.g. a cloud queue, a message broker, or a service bus)
/// </summary>
public interface IQueueStore
{
    Task<Result<long, Error>> CountAsync(string queueName, CancellationToken cancellationToken);

    Task<Result<Error>> DestroyAllAsync(string queueName, CancellationToken cancellationToken);

    Task<Result<bool, Error>> PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken);

    Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken);
}