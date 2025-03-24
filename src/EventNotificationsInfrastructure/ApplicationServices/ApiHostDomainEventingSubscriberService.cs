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
    private readonly IRecorder _recorder;
    private readonly IMessageBusStore _store;
    private readonly Dictionary<Type, string> _subscriptionNames;

    public ApiHostDomainEventingSubscriberService(IRecorder recorder, IConfigurationSettings settings,
        IMessageBusStore store)
    {
        _recorder = recorder;
        _store = store;

        ConsumerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes().Where(type =>
                typeof(IDomainEventNotificationConsumer).IsAssignableFrom(type)
                && type.IsInterface
                && type != typeof(IDomainEventNotificationConsumer)))
            .ToList();
        var subscriptionNamePrefix = settings.Platform.GetString(SubscriptionNameSettingName);
        _subscriptionNames =
            ConsumerTypes.ToDictionary(type => type, type => CreateSubscriptionName(type, subscriptionNamePrefix));
    }

    public IReadOnlyList<Type> ConsumerTypes { get; }

    public async Task<Result<Error>> RegisterAllSubscribersAsync(CancellationToken cancellationToken)
    {
        foreach (var consumerType in ConsumerTypes)
        {
            var subscriptionName = _subscriptionNames.Single(name => name.Key == consumerType).Value;
            var registered = await _store.SubscribeAsync(EventingConstants.Topics.DomainEvents, subscriptionName,
                cancellationToken);
            if (registered.IsFailure)
            {
                return registered.Error;
            }
        }

        return Result.Ok;
    }

    public IReadOnlyList<string> SubscriptionNames => _subscriptionNames.Select(name => name.Value).ToList();

    private static string CreateSubscriptionName(Type consumerType, string prefix)
    {
        var fullName = consumerType.FullName!.Replace(".", "_");
        return $"{prefix}_{fullName}";
    }
}