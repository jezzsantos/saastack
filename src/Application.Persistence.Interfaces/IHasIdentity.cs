using Common;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a type that has a unique identifier
/// </summary>
public interface IHasIdentity
{
    /// <summary>
    ///     Returns the unique ID of the record
    /// </summary>
    Optional<string> Id { get; set; }
}