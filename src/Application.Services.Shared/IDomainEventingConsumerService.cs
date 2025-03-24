using Application.Persistence.Interfaces;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service that notifies consumers of domain events notifications
/// </summary>
public interface IDomainEventingConsumerService
{
    /// <summary>
    ///     Returns all the subscription names for the consumers that are subscribed to consume domain events
    /// </summary>
    public IReadOnlyList<string> SubscriptionNames { get; }

    /// <summary>
    ///     Notifies the subscriber for the specified <see cref="subscriptionName" /> with the specified
    ///     <see cref="changeEvent" />
    /// </summary>
    Task<Result<Error>> NotifySubscriberAsync(string subscriptionName, EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken);
}