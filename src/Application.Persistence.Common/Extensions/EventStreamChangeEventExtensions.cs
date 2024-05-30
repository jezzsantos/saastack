using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;

namespace Application.Persistence.Common.Extensions;

public static class EventStreamChangeEventExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="EventStreamChangeEvent" /> to a <see cref="IDomainEvent" />
    /// </summary>
    public static Result<IDomainEvent, Error> ToDomainEvent(this EventStreamChangeEvent changeEvent,
        IEventSourcedChangeEventMigrator migrator)
    {
        var eventId = changeEvent.Id;
        var eventJson = changeEvent.Data;

        return changeEvent.Metadata.CreateEventFromJson(eventId, eventJson, migrator);
    }
}