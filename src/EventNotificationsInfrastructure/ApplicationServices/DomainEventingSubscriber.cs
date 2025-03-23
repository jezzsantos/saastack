using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Interfaces;

namespace EventNotificationsInfrastructure.ApplicationServices;

/// <summary>
///     Provides message bus subscriber for domain events notifications
/// </summary>
public class DomainEventingSubscriber : IDomainEventingSubscriber
{
    internal const string SubscriptionNameSettingName = "ApplicationServices:EventNotifications:SubscriptionName";
    private readonly IDomainEventNotificationConsumer _consumer;
    private readonly IEventSourcedChangeEventMigrator _migrator;
    private readonly IRecorder _recorder;

    public DomainEventingSubscriber(IRecorder recorder, IConfigurationSettings settings,
        IEventSourcedChangeEventMigrator migrator,
        IDomainEventNotificationConsumer consumer)
    {
        _recorder = recorder;
        _migrator = migrator;
        _consumer = consumer;
        SubscriptionName = CreateSubscriptionName(settings, consumer);
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

    public async Task<Result<Error>> SubscribeAsync(IMessageBusStore store, string topicName,
        CancellationToken cancellationToken)
    {
        return await store.SubscribeAsync(topicName, SubscriptionName, cancellationToken);
    }

    public string SubscriptionName { get; }

    private static string CreateSubscriptionName(IConfigurationSettings settings,
        IDomainEventNotificationConsumer consumer)
    {
        var subscriptionNamePrefix = settings.Platform.GetString(SubscriptionNameSettingName);
        var fullName = consumer.GetType().FullName!.Replace(".", "_");
        return $"{subscriptionNamePrefix}_{fullName}";
    }
}