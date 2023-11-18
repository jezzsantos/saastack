using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Domain.Common.Entities;

public static class EventSourcedChangeEventExtensions
{
    /// <summary>
    ///     Converts the <see cref="changeEvent" /> to a <see cref="IDomainEvent" /> using the <see cref="migrator" /> if
    ///     necessary
    /// </summary>
    public static Result<IDomainEvent, Error> ToEvent(this EventSourcedChangeEvent changeEvent,
        IEventSourcedChangeEventMigrator migrator)
    {
        var metadata = EventMetadata.Create(changeEvent.Metadata);
        return metadata.RehydrateEventFromJson(changeEvent.Id, changeEvent.Data, migrator);
    }
}