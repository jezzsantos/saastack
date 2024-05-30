using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Eventing.Interfaces.Notifications;

/// <summary>
///     Defines relay of domain events
/// </summary>
public interface IDomainEventConsumerRelay
{
    /// <summary>
    ///     Relays the specified <see cref="changeEvent" /> to the specified <see cref="registration" />
    /// </summary>
    Task<Result<Error>> RelayDomainEventAsync(IDomainEvent @event, EventStreamChangeEvent changeEvent,
        CancellationToken cancellationToken);
}