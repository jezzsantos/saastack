using Common;
using Domain.Interfaces.Entities;

namespace Domain.Common.ValueObjects;

public static class EventMetadataExtensions
{
    /// <summary>
    ///     Rehydrates a <see cref="IDomainEvent" /> from the specified <see cref="eventJson" />, using the
    ///     <see cref="IEventSourcedChangeEventMigrator" />
    /// </summary>
    public static Result<IDomainEvent, Error> RehydrateEventFromJson(this EventMetadata metadata, string eventId,
        string eventJson, IEventSourcedChangeEventMigrator migrator)
    {
        var typeName = metadata.Fqn;
        return migrator.Rehydrate(eventId, eventJson, typeName);
    }
}