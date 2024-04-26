using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Domain.Common.Extensions;

public static class EventMetadataExtensions
{
    /// <summary>
    ///     Returns the <see cref="IDomainEvent" /> for the specified <see cref="eventJson" /> using the
    ///     <see cref="migrator" />
    /// </summary>
    public static Result<IDomainEvent, Error> CreateEventFromJson(this EventMetadata metadata, string eventId,
        string eventJson, IEventSourcedChangeEventMigrator migrator)
    {
        eventId.ThrowIfNotValuedParameter(nameof(eventId));
        eventJson.ThrowIfNotValuedParameter(nameof(eventJson));

        var typeName = metadata.Fqn;
        var eventData = eventJson;
        var rehydrated = migrator.Rehydrate(eventId, eventData, typeName);
        if (rehydrated.IsFailure)
        {
            return rehydrated.Error;
        }

        return rehydrated;
    }
}