namespace Application.Services.Shared;

/// <summary>
///     Defines a service that subscribes consumers to domain events notifications
/// </summary>
public interface IDomainEventingSubscriptionService : IDomainEventingConsumerService, IDomainEventingSubscriberService
{
    /// <summary>
    ///     Returns all the subscription names that consumers are subscribed to
    /// </summary>
    public IReadOnlyList<string> SubscriptionNames { get; }
}