using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Domain.Interfaces.Entities;

public class VersionedChangeEvent : IIdentifiableEntity, IQueryableEntity
{
    public VersionedChangeEvent(string id, string entityType, string eventType, string data, string metadata,
        DateTime? lastPersistedAtUtc, int version)
    {
        Id = id;
        EntityType = entityType;
        EventType = eventType;
        Data = data;
        Metadata = metadata;
        LastPersistedAtUtc = lastPersistedAtUtc;
        Version = version;
    }

    public string EntityType { get; set; }

    public string EventType { get; set; }

    public string Data { get; set; }

    public string Metadata { get; set; }

    public DateTime? LastPersistedAtUtc { get; set; }

    public int Version { get; set; }

    public string Id { get; set; }

    Identifier IIdentifiableEntity.Id => Identifier.Create(Id);
}