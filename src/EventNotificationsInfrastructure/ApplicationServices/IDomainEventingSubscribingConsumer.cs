using Application.Persistence.Interfaces;
using Common;
using Infrastructure.Persistence.Interfaces;

namespace EventNotificationsInfrastructure.ApplicationServices;

/// <summary>
///     Defines a consumer that subscribes to domain events notifications
/// </summary>
public interface IDomainEventingSubscribingConsumer
{
    /// <summary>
    ///     Returns the name of the subscription for this subscriber
    /// </summary>
    string SubscriptionName { get; }

    /// <summary>
    ///     Notifies the subscriber of the specified <see cref="changeEvent" />
    /// </summary>
    Task<Result<Error>> NotifyAsync(EventStreamChangeEvent changeEvent, CancellationToken cancellationToken);

    /// <summary>
    ///     Subscribes the consumer to the message bus topic
    /// </summary>
    Task<Result<Error>> SubscribeAsync(IMessageBusStore store, string topicName, CancellationToken cancellationToken);
}