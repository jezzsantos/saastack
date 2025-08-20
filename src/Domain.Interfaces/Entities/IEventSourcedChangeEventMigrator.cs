using Common;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a migrator for migrating older/obsolete versions of <see cref="EventSourcedChangeEvent" /> that no longer
///     exist in the codebase
/// </summary>
public interface IEventSourcedChangeEventMigrator
{
    Result<IDomainEvent, Error> Rehydrate(string eventId, string eventJson, string originalEventTypeName);

    Result<IDomainEvent, Error> Rehydrate(string eventId, IDomainEvent domainEvent);
}