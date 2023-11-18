using Common;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an aggregate root that can be persisted to a stream of events for an event store
/// </summary>
public interface IEventSourcedAggregateRoot : IEventSourcedEntity
{
    IReadOnlyList<IDomainEvent> Events { get; }

    Optional<DateTime> LastPersistedAtUtc { get; }

    Result<Error> ClearChanges();

    Result<List<EventSourcedChangeEvent>, Error> GetChanges();

    Result<Error> LoadChanges(IEnumerable<EventSourcedChangeEvent> history, IEventSourcedChangeEventMigrator migrator);
}