using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace EventNotificationsInfrastructure.ApplicationServices;

/// <summary>
///     Provides a service that notified consumers of domain events notifications
/// </summary>
public class ApiHostDomainEventingConsumerService : IDomainEventingConsumerService
{
    private readonly IReadOnlyList<IDomainEventingSubscribingConsumer> _consumers;
    private readonly IDomainEventingSubscriberService _subscriberService;

    public ApiHostDomainEventingConsumerService(IRecorder recorder,
        IEnumerable<IDomainEventNotificationConsumer> consumers, IEventSourcedChangeEventMigrator migrator,
        IDomainEventingSubscriberService subscriberService) : this(subscriberService,
        WrapConsumers(recorder, subscriberService, consumers, migrator))
    {
    }

    internal ApiHostDomainEventingConsumerService(IDomainEventingSubscriberService subscriberService,
        IReadOnlyList<IDomainEventingSubscribingConsumer> consumers)
    {
        _subscriberService = subscriberService;
        _consumers = consumers;
    }

    public async Task<Result<Error>> NotifySubscriberAsync(string subscriptionName, EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken)
    {
        var consumer =
            _consumers.FirstOrDefault(consumer => consumer.SubscriptionName.EqualsIgnoreCase(subscriptionName));
        if (consumer.NotExists())
        {
            throw new InvalidOperationException(Resources
                .ApiHostDomainEventingConsumerService_NotifySubscriberAsync_MissingConsumer
                .Format(subscriptionName));
        }

        return await consumer.NotifyAsync(changeEvent, cancellationToken);
    }

    public IReadOnlyList<string> SubscriptionNames => _subscriberService.SubscriptionNames;

    private static List<IDomainEventingSubscribingConsumer> WrapConsumers(IRecorder recorder,
        IDomainEventingSubscriberService subscriberService, IEnumerable<IDomainEventNotificationConsumer> consumers,
        IEventSourcedChangeEventMigrator migrator)
    {
        var injectedConsumers = consumers.ToList();
        var registeredConsumers = subscriberService.Consumers;
        var foundConsumersTypes = injectedConsumers.Select(consumer => consumer.GetType()).ToList();
        var registeredConsumerTypes = registeredConsumers.Select(consumer => consumer.Key).ToList();
        var missingFromRegistration = foundConsumersTypes.Except(registeredConsumerTypes).ToList();
        var missingFromInjection = registeredConsumerTypes.Except(foundConsumersTypes).ToList();
        if (missingFromRegistration.HasAny())
        {
            var notRegistered = missingFromRegistration.Select(type => type.FullName).Join(",");
            throw new InvalidOperationException(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromRegistration.Format(
                    notRegistered));
        }

        if (missingFromInjection.HasAny())
        {
            var notInjected = missingFromInjection.Select(type => type.FullName).Join(",");
            throw new InvalidOperationException(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromInjection.Format(notInjected));
        }

        return injectedConsumers
            .Select(consumer =>
            {
                var registeredConsumer = registeredConsumers.Single(rc => rc.Key == consumer.GetType());
                return new ApiHostDomainEventingSubscribingConsumer(recorder, registeredConsumer.Value, migrator,
                    consumer);
            })
            .ToList<IDomainEventingSubscribingConsumer>();
    }

    public interface IDomainEventingSubscribingConsumer
    {
        string SubscriptionName { get; }

        Task<Result<Error>> NotifyAsync(EventStreamChangeEvent changeEvent,
            CancellationToken cancellationToken);
    }

    /// <summary>
    ///     Provides message bus subscriber for domain events notifications
    /// </summary>
    internal class ApiHostDomainEventingSubscribingConsumer : IDomainEventingSubscribingConsumer
    {
        private readonly IDomainEventNotificationConsumer _consumer;
        private readonly IEventSourcedChangeEventMigrator _migrator;
        private readonly IRecorder _recorder;

        public ApiHostDomainEventingSubscribingConsumer(IRecorder recorder, string subscriptionName,
            IEventSourcedChangeEventMigrator migrator, IDomainEventNotificationConsumer consumer)
        {
            _recorder = recorder;
            SubscriptionName = subscriptionName;
            _migrator = migrator;
            _consumer = consumer;
        }

        public async Task<Result<Error>> NotifyAsync(EventStreamChangeEvent changeEvent,
            CancellationToken cancellationToken)
        {
            var converted = changeEvent.ToDomainEvent(_migrator);
            if (converted.IsFailure)
            {
                return converted.Error;
            }

            var domainEvent = converted.Value;
            var notified = await _consumer.NotifyAsync(domainEvent, cancellationToken);
            if (notified.IsFailure)
            {
                var consumerName = _consumer.GetType().Name;
                var eventId = domainEvent.RootId;
                var eventName = changeEvent.Metadata.Fqn;
                var ex = notified.Error.ToException<InvalidOperationException>();
                _recorder.Crash(null, CrashLevel.Critical, ex,
                    "Consumer {Consumer} failed to process event {EventId} ({EventType})",
                    consumerName, eventId, eventName);
                return notified.Error.Wrap(ErrorCode.Unexpected,
                    Resources.DomainEventingSubscriber_ConsumerFailed.Format(consumerName, eventId, eventName));
            }

            return Result.Ok;
        }

        public string SubscriptionName { get; }
    }
}