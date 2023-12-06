using Common;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an aggregate root that produces a stream of change events
/// </summary>
public interface IChangeEventProducingAggregateRoot
{
    IReadOnlyList<IDomainEvent> Events { get; }

    Optional<DateTime> LastPersistedAtUtc { get; }

    Result<Error> ClearChanges();

    Result<List<EventSourcedChangeEvent>, Error> GetChanges();
}