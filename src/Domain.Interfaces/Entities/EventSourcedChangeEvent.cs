using Common;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a <see cref="IDomainEvent" /> that is persisted as an event sourced event, in an event store
/// </summary>
public struct EventSourcedChangeEvent : IIdentifiableEntity, IQueryableEntity
{
    public string Data { get; private init; }

    public string EntityType { get; private init; }

    public string EventType { get; private init; }

    public ISingleValueObject<string> Id { get; private init; }

    public DateTime? LastPersistedAtUtc { get; set; }

    public string Metadata { get; private init; }

    public int Version { get; private init; }

    public static Result<EventSourcedChangeEvent, Error> Create(
        Func<IIdentifiableEntity, Result<ISingleValueObject<string>, Error>> idFactory, string entityType,
        string eventType, string jsonData, string eventMetadata, int version)
    {
        var identifier = idFactory(new EventSourcedChangeEvent());
        return identifier.Match<Result<EventSourcedChangeEvent, Error>>(id => new EventSourcedChangeEvent
        {
            Id = id.Value,
            EntityType = entityType,
            EventType = eventType,
            Data = jsonData,
            Metadata = eventMetadata,
            Version = version
        }, error => error);
    }

    public static EventSourcedChangeEvent Create(ISingleValueObject<string> id, string entityType,
        string eventType, string jsonData, string eventMetadata, int version)
    {
        return new EventSourcedChangeEvent
        {
            Id = id,
            EntityType = entityType,
            EventType = eventType,
            Data = jsonData,
            Metadata = eventMetadata,
            Version = version
        };
    }
}