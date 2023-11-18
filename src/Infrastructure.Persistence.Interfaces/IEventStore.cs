using Common;
using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines read/write access to streams of events of an entity to and from an event store
///     (e.g. a database (relational or not), or an event store)
/// </summary>
public interface IEventStore
{
    Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId, List<EventSourcedChangeEvent> events,
        CancellationToken cancellationToken);

    Task<Result<Error>> DestroyAllAsync(string entityName, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync(string entityName, string entityId,
        CancellationToken cancellationToken);
}