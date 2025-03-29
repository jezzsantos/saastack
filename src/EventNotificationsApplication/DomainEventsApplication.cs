using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using EventNotificationsApplication.Persistence;
using EventNotification = Application.Resources.Shared.EventNotification;

namespace EventNotificationsApplication;

public class DomainEventsApplication : IDomainEventsApplication
{
    private readonly IRecorder _recorder;
    private readonly IEventNotificationRepository _eventNotificationRepository;
    private readonly IDomainEventConsumerService _domainEventConsumerService;
#if TESTINGONLY
    private readonly IDomainEventingMessageBusTopic _domainEventMessageBusTopic;
    private readonly IDomainEventingSubscriber _subscriber;

    public DomainEventsApplication(IRecorder recorder,
        IEventNotificationRepository eventNotificationRepository,
        IDomainEventingMessageBusTopic domainEventMessageBusTopic,
        IDomainEventingSubscriber subscriber, IDomainEventConsumerService domainEventConsumerService)
    {
        _recorder = recorder;
        _eventNotificationRepository = eventNotificationRepository;
        _domainEventMessageBusTopic = domainEventMessageBusTopic;
        _subscriber = subscriber;
        _domainEventConsumerService = domainEventConsumerService;
    }
#else
    public DomainEventsApplication(IRecorder recorder,
        IEventNotificationRepository eventNotificationRepository,
        // ReSharper disable once UnusedParameter.Local
        IDomainEventingMessageBusTopic domainEventMessageBusTopic,
        // ReSharper disable once UnusedParameter.Local
        IDomainEventingSubscriber subscriber, IDomainEventConsumerService domainEventConsumerService)
    {
        _recorder = recorder;
        _eventNotificationRepository = eventNotificationRepository;
        _domainEventConsumerService = domainEventConsumerService;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllDomainEventsAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        await DrainAllOnSubscriptionAsync(_domainEventMessageBusTopic, _subscriber.SubscriptionName,
            message => NotifyDomainEventInternalAsync(caller, message, cancellationToken), cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all domain event messages");

        return Result.Ok;
    }
#endif

    public async Task<Result<bool, Error>> NotifyDomainEventAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<DomainEventingMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered = await NotifyDomainEventInternalAsync(caller, rehydrated.Value, cancellationToken);
        if (delivered.IsFailure)
        {
            return delivered.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Delivered eventing message: {Message}", messageAsJson);
        return true;
    }

#if TESTINGONLY
    public async Task<Result<SearchResults<EventNotification>, Error>>
        SearchAllNotificationsAsync(ICallerContext caller, SearchOptions searchOptions, GetOptions getOptions,
            CancellationToken cancellationToken)
    {
        var searched = await _eventNotificationRepository.SearchAllAsync(searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var events = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All notifications were fetched");

        return events.ToSearchResults(searchOptions, evt => evt.ToNotification());
    }
#endif

#if TESTINGONLY
    private static async Task DrainAllOnSubscriptionAsync<TBusMessage>(IMessageBusTopicStore<TBusMessage> repository,
        string subscriptionName, Func<TBusMessage, Task<Result<bool, Error>>> handler,
        CancellationToken cancellationToken)
        where TBusMessage : IQueuedMessage, new()
    {
        var found = new Result<bool, Error>(true);
        while (found.Value)
        {
            found = await repository.ReceiveSingleAsync(subscriptionName, OnMessageReceivedAsync, cancellationToken);
            continue;

            async Task<Result<Error>> OnMessageReceivedAsync(TBusMessage message, CancellationToken _)
            {
                var handled = await handler(message);
                if (handled.IsFailure)
                {
                    handled.Error.Throw<InvalidOperationException>();
                }

                return Result.Ok;
            }
        }
    }
#endif

    private static Result<TBusMessage, Error> RehydrateMessage<TBusMessage>(string messageAsJson)
        where TBusMessage : IQueuedMessage
    {
        try
        {
            var message = messageAsJson.FromJson<TBusMessage>();
            if (message.NotExists())
            {
                return Error.RuleViolation(
                    Resources.DomainEventsApplication_InvalidBusMessage.Format(typeof(TBusMessage).Name,
                        messageAsJson));
            }

            return message;
        }
        catch (Exception)
        {
            return Error.RuleViolation(
                Resources.DomainEventsApplication_InvalidBusMessage.Format(typeof(TBusMessage).Name,
                    messageAsJson));
        }
    }

    private async Task<Result<bool, Error>> NotifyDomainEventInternalAsync(ICallerContext caller,
        DomainEventingMessage message, CancellationToken cancellationToken)
    {
        var subscriber = _domainEventConsumerService.GetSubscriber();
        var notification = message.ToNotification(subscriber);
        var added = await _eventNotificationRepository.SaveAsync(notification, cancellationToken);
        if (added.IsFailure)
        {
            return added.Error;
        }

        var notified = await _domainEventConsumerService.NotifyAsync(message.Event!, cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Notified domain event for {EventType}:v{Version}",
            notification.Metadata.Value.Fqn, notification.Version);

        return true;
    }
}

public static class DomainEventConversionExtensions
{
    public static Persistence.ReadModels.EventNotification ToNotification(this DomainEventingMessage message,
        string subscriberRef)
    {
        return new Persistence.ReadModels.EventNotification
        {
            Id = message.Event!.Id,
            RootAggregateType = message.Event.RootAggregateType,
            EventType = message.Event.EventType,
            Data = message.Event.Data,
            Metadata = message.Event.Metadata,
            StreamName = message.Event.StreamName,
            Version = message.Event.Version,
            SubscriberRef = subscriberRef
        };
    }

    public static EventNotification ToNotification(this Persistence.ReadModels.EventNotification notification)
    {
        return new EventNotification
        {
            Id = notification.Id,
            RootAggregateType = notification.RootAggregateType,
            EventType = notification.EventType,
            Data = notification.Data,
            MetadataFullyQualifiedName = notification.Metadata.Value.Fqn,
            StreamName = notification.StreamName,
            Version = notification.Version,
            SubscriberRef = notification.SubscriberRef
        };
    }
}