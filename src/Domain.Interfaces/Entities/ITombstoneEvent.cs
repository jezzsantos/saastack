namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a special domain event for ending the lifetime of an aggregate
/// </summary>
public interface ITombstoneEvent : IDomainEvent
{
    string DeletedById { get; set; }

    bool IsTombstone { get; set; }
}