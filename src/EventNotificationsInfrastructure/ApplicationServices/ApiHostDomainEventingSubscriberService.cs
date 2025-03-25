using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Interfaces;

namespace EventNotificationsInfrastructure.ApplicationServices;

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
        var subscriptionNamePrefix = settings.Platform.GetString(SubscriptionNameSettingName);
        _consumers =
            consumerTypes.ToDictionary(type => type, type => CreateSubscriptionName(type, subscriptionNamePrefix));
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
                               && type != typeof(IDomainEventNotificationConsumer)))
            .ToList();
    }

    private static string CreateSubscriptionName(Type consumerType, string prefix)
    {
        var fullName = consumerType.FullName!.Replace(".", "_");
        return $"{prefix}_{fullName}";
    }
}