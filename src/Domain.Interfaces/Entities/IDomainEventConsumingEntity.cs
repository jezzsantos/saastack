using Common;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an entity/aggregate that can change its state from handling a <see cref="IDomainEvent" />
/// </summary>
public interface IDomainEventConsumingEntity : IIdentifiableEntity
{
    Result<Error> HandleStateChanged(IDomainEvent @event);
}