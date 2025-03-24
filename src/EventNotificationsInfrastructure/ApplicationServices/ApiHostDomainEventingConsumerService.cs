using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace EventNotificationsInfrastructure.ApplicationServices;

/// <summary>
///     Provides a service that notified consumers of domain events notifications
/// </summary>
public class ApiHostDomainEventingConsumerService : IDomainEventingConsumerService
{
    private readonly IReadOnlyList<IDomainEventingSubscribingConsumer> _consumers;
    private readonly IDomainEventingSubscriberService _domainEventingSubscriberService;

    public ApiHostDomainEventingConsumerService(IRecorder recorder, IConfigurationSettings settings,
        IEnumerable<IDomainEventNotificationConsumer> consumers, IEventSourcedChangeEventMigrator migrator,
        IDomainEventingSubscriberService domainEventingSubscriberService)
    {
        _domainEventingSubscriberService = domainEventingSubscriberService;
        _consumers = consumers
            .Select(c => new ApiHostDomainEventingSubscribingConsumer(recorder, settings, migrator, c))
            .ToList();

        //TODO: Should we need to check whether the actual assembly consumers are any different from what get injected into this constructor?
    }

    public async Task<Result<Error>> NotifySubscriberAsync(string subscriptionName, EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        var subscriber =
            _consumers.FirstOrDefault(sub => sub.SubscriptionName.EqualsIgnoreCase(subscriptionName));
        if (subscriber.NotExists())
        {
            return
                Error.PreconditionViolation(); //TODO: What to do here. Basically we have a change event for a subscription but no consumer to handle it
        }

        return await subscriber.NotifyAsync(changeEvent, cancellationToken);
    }

    public IReadOnlyList<string> SubscriptionNames => _domainEventingSubscriberService.SubscriptionNames;
}