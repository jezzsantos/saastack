using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a subscriber to domain events
/// </summary>
public interface IDomainEventingSubscriber
{
    /// <summary>
    ///     The name of the subscriber
    /// </summary>
    string SubscriptionName { get; }

    /// <summary>
    ///     Subscribes the subscriber to receive domain events
    /// </summary>
    Task<Result<Error>> Subscribe(CancellationToken cancellationToken);
}