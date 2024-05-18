using Common;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a versioned <see cref="IDomainEvent" /> that is persisted as an event sourced event, in an event store,
///     used to define the change events to and from event sourced aggregates.
///     Note: We are delegating the value of <see cref="Id" /> to be the value to the <see cref="IIdentifiableEntity.Id" />
///     (using the private <see cref="Identifier" /> class) as a convenient workaround, to avoid requiring the mapping of
///     from domain properties
/// </summary>
public struct EventSourcedChangeEvent : IIdentifiableEntity, IQueryableEntity
{
    private readonly Identifier _identifier;

    public static Result<EventSourcedChangeEvent, Error> Create(
        Func<IIdentifiableEntity, Result<ISingleValueObject<string>, Error>> idFactory, string entityType,
        bool isTombstone,
        string eventType, string jsonData, string eventMetadata, int version)
    {
        var identifier = idFactory(new EventSourcedChangeEvent());
        return identifier.Match<Result<EventSourcedChangeEvent, Error>>(id =>
            new EventSourcedChangeEvent(id.Value.Value, jsonData, entityType, isTombstone, eventType, eventMetadata)
            {
                Version = version
            }, error => error);
    }

    public static EventSourcedChangeEvent Create(ISingleValueObject<string> id, string entityType, bool isTombstone,
        string eventType, string jsonData, string eventMetadata, int version)
    {
        return new EventSourcedChangeEvent(id.Value, jsonData, entityType, isTombstone, eventType, eventMetadata)
        {
            Version = version
        };
    }

    private EventSourcedChangeEvent(string id, string data, string entityType, bool isTombstone, string eventType,
        string metadata)
    {
        _identifier = new Identifier(id);
        Id = id;
        Data = data;
        EntityType = entityType;
        EventType = eventType;
        Metadata = metadata;
        IsTombstone = isTombstone;
    }

    public string Data { get; private init; }

    public string EntityType { get; private init; }

    public string EventType { get; private init; }

    public string Id { get; private init; }

    ISingleValueObject<string> IIdentifiableEntity.Id => _identifier;

    public Optional<DateTime> LastPersistedAtUtc { get; set; }

    public string Metadata { get; private init; }

    public int Version { get; private init; }

    public bool IsTombstone { get; private init; }

    private readonly struct Identifier : ISingleValueObject<string>
    {
        public Identifier(string id)
        {
            Value = id;
        }

        public string Dehydrate()
        {
            return Value;
        }

        public string Value { get; }
    }
}