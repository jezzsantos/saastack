using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common.ApplicationServices;

namespace Infrastructure.Persistence.Common.Extensions;

public static class EventSourcedChangeEventExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="@event" /> to an <see cref="EventStoreEntity" />
    /// </summary>
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