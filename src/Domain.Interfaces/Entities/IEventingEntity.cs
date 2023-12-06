namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an entity that is event sourced
/// </summary>
public interface IEventingEntity : IDomainEventProducingEntity, IDomainEventConsumingEntity
{
    DateTime CreatedAtUtc { get; }

    DateTime LastModifiedAtUtc { get; }
}