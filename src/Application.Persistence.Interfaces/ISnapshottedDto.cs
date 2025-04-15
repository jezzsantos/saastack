using Common;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a DTO that is snapshotted
/// </summary>
public interface ISnapshottedDto
{
    /// <summary>
    ///     Returns the date that the record was created
    /// </summary>
    Optional<DateTime> CreatedAtUtc { get; set; }

    /// <summary>
    ///     Returns the date that the record was last modified
    /// </summary>
    Optional<DateTime> LastModifiedAtUtc { get; set; }
}