namespace Domain.Interfaces.Entities;

public interface IEventSourcedEntity : IDomainEventProducingEntity, IDomainEventConsumingEntity
{
    DateTime CreatedAtUtc { get; }

    DateTime LastModifiedAtUtc { get; }
}