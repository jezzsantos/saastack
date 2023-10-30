namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a domain event to communicate past events of an aggregate
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    ///     Returns the time when the event happened
    /// </summary>
    DateTime OccurredUtc { get; set; }

    /// <summary>
    ///     Returns the ID of the root aggregate
    /// </summary>
    string RootId { get; set; }
}