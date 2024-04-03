using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines a translator of domain events from a domain aggregate root to integration events
/// </summary>
public interface IIntegrationEventNotificationTranslator
{
    /// <summary>
    ///     Returns the type of the root aggregate that produces the domain events
    /// </summary>
    Type RootAggregateType { get; }

    /// <summary>
    ///     Handles the notification of a new <see cref="IDomainEvent" />, and returns an optional <see cref="IIntegrationEvent" />
    ///     event to be published to downstream consumers of integration events
    /// </summary>
    Task<Result<Optional<IIntegrationEvent>, Error>> TranslateAsync(IDomainEvent domainEvent,
        CancellationToken cancellationToken);
}