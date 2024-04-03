using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a translator of domain events that never returns an integration event
/// </summary>
public sealed class NoOpIntegrationEventNotificationTranslator<TAggregateRoot> : IIntegrationEventNotificationTranslator
    where TAggregateRoot : IEventingAggregateRoot
{
    public Type RootAggregateType => typeof(TAggregateRoot);

    public Task<Result<Optional<IIntegrationEvent>, Error>> TranslateAsync(IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<Optional<IIntegrationEvent>, Error>>(Optional<IIntegrationEvent>.None);
    }
}