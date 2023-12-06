namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an aggregate root that produces a set of change events, and can be initialized with a stream of events
/// </summary>
public interface IEventingAggregateRoot : IChangeEventConsumingAggregateRoot, IChangeEventProducingAggregateRoot,
    IEventingEntity
{
}