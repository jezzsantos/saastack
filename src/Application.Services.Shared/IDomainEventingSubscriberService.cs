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
    IReadOnlyDictionary<Type, string> Consumers { get; }

    /// <summary>
    ///     Returns all the names of all subscriptions that consume domain events
    /// </summary>
    IReadOnlyList<string> SubscriptionNames { get; }

    /// <summary>
    ///     Registers all subscribers to the message bus topic for consuming domain events
    /// </summary>
    Task<Result<Error>> RegisterAllSubscribersAsync(CancellationToken cancellationToken);
}