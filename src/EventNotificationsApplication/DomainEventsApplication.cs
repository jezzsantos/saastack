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
    private readonly IDomainEventingConsumerService _domainEventingConsumerService;
#if TESTINGONLY
    private readonly IDomainEventingMessageBusTopic _domainEventMessageBusTopic;

    public DomainEventsApplication(IRecorder recorder,
        IEventNotificationRepository eventNotificationRepository,
        IDomainEventingMessageBusTopic domainEventMessageBusTopic,
        IDomainEventingConsumerService domainEventingConsumerService)
    {
        _recorder = recorder;
        _eventNotificationRepository = eventNotificationRepository;
        _domainEventMessageBusTopic = domainEventMessageBusTopic;
        _domainEventingConsumerService = domainEventingConsumerService;
    }
#else
    public DomainEventsApplication(IRecorder recorder,
        IEventNotificationRepository eventNotificationRepository,
        // ReSharper disable once UnusedParameter.Local
        IDomainEventingMessageBusTopic domainEventMessageBusTopic,
        IDomainEventingConsumerService domainEventingConsumerService)
    {
        _recorder = recorder;
        _eventNotificationRepository = eventNotificationRepository;
        _domainEventingConsumerService = domainEventingConsumerService;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DrainAllDomainEventsAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        await DrainAllOnTopicAsync(_recorder, _domainEventingConsumerService, _domainEventMessageBusTopic,
            (subscriptionName, message) =>
                NotifyDomainEventInternalAsync(caller, subscriptionName, message, cancellationToken),
            cancellationToken);

        _recorder.TraceInformation(caller.ToCall(), "Drained all domain event messages for all subscriptions");

        return Result.Ok;
    }
#endif

    public async Task<Result<bool, Error>> NotifyDomainEventAsync(ICallerContext caller, string subscriptionName,
        string messageAsJson, CancellationToken cancellationToken)
    {
        var rehydrated = RehydrateMessage<DomainEventingMessage>(messageAsJson);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        var delivered =
            await NotifyDomainEventInternalAsync(caller, subscriptionName, rehydrated.Value, cancellationToken);
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
    /// <summary>
    ///     Round robins all subscriptions until no messages are left on the topic for that subscription.
    ///     We need to keep going again and again until all subscriptions are empty otherwise,
    ///     during handling of a subscriptions, the consumers might produce more messages,
    ///     that would need to be processed by other subscriptions, that have already been checked.
    /// </summary>
    private static async Task DrainAllOnTopicAsync<TBusMessage>(IRecorder recorder,
        IDomainEventingConsumerService domainEventingConsumerService,
        IMessageBusTopicStore<TBusMessage> messageBusTopic,
        Func<string, TBusMessage, Task<Result<bool, Error>>> handler, CancellationToken cancellationToken)
        where TBusMessage : IQueuedMessage, new()
    {
        var maxGenerations = 5;
        var subscriptionNames = domainEventingConsumerService.SubscriptionNames;
        bool foundAnyMessages;
        do
        {
            foundAnyMessages = false;
            await DrainEachSubscription();
            maxGenerations--;
        } while (foundAnyMessages && maxGenerations > 1);

        return;

        async Task DrainEachSubscription()
        {
            foreach (var subscriptionName in subscriptionNames)
            {
                var found = new Result<bool, Error>(true);
                while (found.Value)
                {
                    found = await messageBusTopic.ReceiveSingleAsync(subscriptionName, OnMessageReceivedAsync,
                        cancellationToken);
                    if (found.IsFailure)
                    {
                        recorder.TraceError(null,
                            "Failed to receive message for subscription {Subscription}. Error was: {Error}",
                            subscriptionName, found.Error.Message);
                        continue;
                    }

                    if (found.Value)
                    {
                        foundAnyMessages = true;
                    }

                    continue;

                    async Task<Result<Error>> OnMessageReceivedAsync(TBusMessage message, CancellationToken _)
                    {
                        var handled = await handler(subscriptionName, message);
                        if (handled.IsFailure)
                        {
                            handled.Error.Throw<InvalidOperationException>();
                        }

                        return Result.Ok;
                    }
                }
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
        string subscriptionName, DomainEventingMessage message, CancellationToken cancellationToken)
    {
        var notification = message.ToNotification(subscriptionName);
        var added = await _eventNotificationRepository.SaveAsync(notification, cancellationToken);
        if (added.IsFailure)
        {
            return added.Error;
        }

        var notified =
            await _domainEventingConsumerService.NotifySubscriberAsync(subscriptionName, message.Event!,
                cancellationToken);
        if (notified.IsFailure)
        {
            return notified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Notified domain event for {Subscription} for {EventType}:v{Version}",
            subscriptionName, notification.Metadata.Value.Fqn, notification.Version);

        return true;
    }
}

public static class DomainEventConversionExtensions
{
    public static Persistence.ReadModels.EventNotification ToNotification(this DomainEventingMessage message,
        string subscriptionName)
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
            SubscriberRef = subscriptionName
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