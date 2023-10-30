using Common;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an entity/aggregate that can change its state by raising <see cref="IDomainEvent" />
/// </summary>
public interface IDomainEventProducingEntity
{
    Result<Error> RaiseEvent(IDomainEvent @event, bool validate);
}