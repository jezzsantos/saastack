using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Infrastructure.Persistence.Interfaces;

namespace EventNotificationsInfrastructure.ApplicationServices;

public class ApiHostDomainEventingSubscriber : IDomainEventingSubscriber
{
    private const string EventingSubscriberNameSettingName = "ApplicationServices:EventNotifications:SubscriptionName";
    private readonly IMessageBusStore _store;

    public ApiHostDomainEventingSubscriber(IConfigurationSettings settings, IMessageBusStore store)
    {
        _store = store;
        SubscriptionName = settings.Platform.GetString(EventingSubscriberNameSettingName);
    }

    public async Task<Result<Error>> Subscribe(CancellationToken cancellationToken)
    {
        return await _store.SubscribeAsync(EventingConstants.Topics.DomainEvents, SubscriptionName, cancellationToken);
    }

    public string SubscriptionName { get; }
}