using Application.Interfaces;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Interfaces;

namespace EventNotificationsInfrastructure.ApplicationServices;

/// <summary>
///     Provides a service that subscribes consumers to domain events notifications
/// </summary>
public class ApiHostDomainEventingSubscriptionService : IDomainEventingSubscriptionService
{
    private readonly IMessageBusStore _store;
    private readonly IReadOnlyList<IDomainEventingSubscriber> _subscribers;

    public ApiHostDomainEventingSubscriptionService(IRecorder recorder, IConfigurationSettings settings,
        IEnumerable<IDomainEventNotificationConsumer> consumers, IEventSourcedChangeEventMigrator migrator,
        IMessageBusStore store)
    {
        _subscribers = consumers
            .Select(c => new DomainEventingSubscriber(recorder, settings, migrator, c))
            .ToList();
        _store = store;
    }

    public async Task<Result<Error>> NotifySubscriberAsync(string subscriptionName, EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        var subscriber = _subscribers.FirstOrDefault(sub => sub.SubscriptionName.EqualsIgnoreCase(subscriptionName));
        if (subscriber.NotExists())
        {
            return Error.PreconditionViolation();
        }

        return await subscriber.NotifyAsync(changeEvent, cancellationToken);
    }

    public async Task<Result<Error>> RegisterAllSubscribersAsync(CancellationToken cancellationToken)
    {
        var subscribed = await Tasks.WhenAllAsync(_subscribers.Select(sub =>
                sub.SubscribeAsync(_store, EventingConstants.Topics.DomainEvents, cancellationToken))
            .ToArray());
        if (subscribed.IsFailure)
        {
            return subscribed.Error;
        }

        return Result.Ok;
    }

    public IReadOnlyList<string> SubscriptionNames => _subscribers.Select(c => c.SubscriptionName).ToList();
}