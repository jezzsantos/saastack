using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Infrastructure.Eventing.Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Interfaces;

namespace EventNotificationsInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="IDomainEventingSubscriberService" /> that registers all
///     <see cref="IDomainEventNotificationConsumer" /> found in the running process of this instance.
///     Note: We want this instance to be registered in IOC container as a singleton so we can call
///     <see cref="IDomainEventingSubscriberService.RegisterAllSubscribersAsync" /> at startup of the host process,
///     with all <see cref="IDomainEventNotificationConsumer" /> types found in all assemblies.
/// </summary>
public class ApiHostDomainEventingSubscriberService : IDomainEventingSubscriberService
{
    internal const string SubscriptionNameSettingName = "ApplicationServices:EventNotifications:SubscriptionName";
    private readonly Dictionary<Type, string> _consumers;
    private readonly IRecorder _recorder;
    private readonly IMessageBusStore _store;

    public ApiHostDomainEventingSubscriberService(IRecorder recorder, IConfigurationSettings settings,
        IMessageBusStore store) : this(recorder, settings, store, GetConsumerTypes())
    {
    }

    internal ApiHostDomainEventingSubscriberService(IRecorder recorder, IConfigurationSettings settings,
        IMessageBusStore store, IReadOnlyList<Type> consumerTypes)
    {
        _recorder = recorder;
        _store = store;
        var assemblyName = settings.Platform.GetString(SubscriptionNameSettingName);
        _consumers =
            consumerTypes.ToDictionary(type => type, type => CreateConsumerName(type, assemblyName));
    }

    public IReadOnlyDictionary<Type, string> Consumers => _consumers;

    public async Task<Result<Error>> RegisterAllSubscribersAsync(CancellationToken cancellationToken)
    {
        foreach (var consumerType in Consumers)
        {
            var subscriptionName = _consumers.Single(name => name.Key == consumerType.Key).Value;
            var registered = await _store.SubscribeAsync(EventingConstants.Topics.DomainEvents, subscriptionName,
                cancellationToken);
            if (registered.IsFailure)
            {
                return registered.Error;
            }

            _recorder.TraceInformation(null, "Registered consumer {ConsumerType} with topic {Topic}", consumerType,
                EventingConstants.Topics.DomainEvents);
        }

        return Result.Ok;
    }

    public IReadOnlyList<string> SubscriptionNames => _consumers.Select(consumer => consumer.Value).ToList();

    private static List<Type> GetConsumerTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes()
                .Where(type => typeof(IDomainEventNotificationConsumer).IsAssignableFrom(type)
                               && type.IsClass
                               && (IsPublic(type) || IsInternal(type))
                               && type != typeof(IDomainEventNotificationConsumer)))
            .ToList();

        bool IsPublic(Type type)
        {
            return type.IsVisible
                   && type.IsPublic
                   && !type.IsNotPublic
                   && !type.IsNested;
        }

        bool IsInternal(Type type)
        {
            return !type.IsVisible
                   && !type.IsPublic
                   && type.IsNotPublic
                   && !type.IsNested;
        }
    }

    private static string CreateConsumerName(Type consumerType, string assemblyName)
    {
        return EventingExtensions.CreateConsumerName(consumerType.FullName!, assemblyName);
    }
}