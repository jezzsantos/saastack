using Application.Persistence.Common;
using Common;
using Domain.Interfaces.Entities;
using QueryAny;

namespace Infrastructure.Persistence.Common.ApplicationServices;

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

public static class DataStoreEventEntityExtensions
{
    public static EventStoreEntity ToTabulated(this EventSourcedChangeEvent @event, string entityName,
        string streamName)
    {
        var dto = new EventStoreEntity
        {
            Id = @event.Id.ToOptional(),
            LastPersistedAtUtc = @event.LastPersistedAtUtc,
            IsDeleted = Optional<bool>.None,
            StreamName = streamName,
            Version = @event.Version,
            EventType = @event.EventType,
            EntityType = @event.EntityType,
            EntityName = entityName,
            Data = @event.Data,
            Metadata = @event.Metadata
        };

        return dto;
    }
}