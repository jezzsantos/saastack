using Common;
using QueryAny;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a DTO that can be persisted
/// </summary>
public interface IPersistableDto : IHasIdentity, IQueryableEntity
{
    /// <summary>
    ///     Whether the record is soft-deleted in the store or not
    /// </summary>
    Optional<bool> IsDeleted { get; }

    /// <summary>
    ///     Returns the date that the record was last persisted to a store
    /// </summary>
    Optional<DateTime> LastPersistedAtUtc { get; }
}