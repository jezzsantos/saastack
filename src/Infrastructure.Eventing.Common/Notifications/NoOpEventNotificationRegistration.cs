using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a registration that handles no integration events
/// </summary>
public class NoOpEventNotificationRegistration<TAggregateRoot> : IEventNotificationRegistration
    where TAggregateRoot : IEventingAggregateRoot
{
    public IIntegrationEventNotificationTranslator IntegrationEventTranslator =>
        new NoOpIntegrationEventNotificationTranslator<TAggregateRoot>();
}