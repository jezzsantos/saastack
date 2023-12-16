using Application.Persistence.Common;
using Common;
using QueryAny;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Provides an entity for storing in an Event Store
/// </summary>
[EntityName("EventStore")]
public class EventStoreEntity : ReadModelEntity
{
    public Optional<string> Data { get; set; }

    public Optional<string> EntityName { get; set; }

    public Optional<string> EntityType { get; set; }

    public Optional<string> EventType { get; set; }

    public Optional<string> Metadata { get; set; }

    public Optional<string> StreamName { get; set; }

    public int Version { get; set; }
}