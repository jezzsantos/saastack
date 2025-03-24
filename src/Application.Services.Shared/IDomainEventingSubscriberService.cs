using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service that handles subscribers of domain events notifications
/// </summary>
public interface IDomainEventingSubscriberService
{
    /// <summary>
    ///     Returns all the consumers that are subscribed to consume domain events
    /// </summary>
    public IReadOnlyList<Type> ConsumerTypes { get; }

    /// <summary>
    ///     Returns all the subscription names for the consumers that are subscribed to consume domain events
    /// </summary>
    public IReadOnlyList<string> SubscriptionNames { get; }

    /// <summary>
    ///     Registers all subscribers to the message bus topic for consuming domain events
    /// </summary>
    Task<Result<Error>> RegisterAllSubscribersAsync(CancellationToken cancellationToken);
}