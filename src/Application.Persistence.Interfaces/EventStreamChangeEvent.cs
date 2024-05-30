using Domain.Common.ValueObjects;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a changed event in an event stream
/// </summary>
public class EventStreamChangeEvent
{
    public required string Data { get; set; }

    public required string EventType { get; set; }

    public required string Id { get; set; }

    public DateTime? LastPersistedAtUtc { get; set; }

    public required EventMetadata Metadata { get; set; }

    public required string RootAggregateType { get; set; }

    public required string StreamName { get; set; }

    public required int Version { get; set; }
}