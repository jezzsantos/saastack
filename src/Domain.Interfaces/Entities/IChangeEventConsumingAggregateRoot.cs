using Common;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an aggregate root that can be initialized (sourced) with a stream of events
/// </summary>
public interface IChangeEventConsumingAggregateRoot
{
    Result<Error> LoadChanges(IEnumerable<EventSourcedChangeEvent> history, IEventSourcedChangeEventMigrator migrator);
}