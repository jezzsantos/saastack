using Domain.Interfaces.Entities;

namespace Domain.Common;

/// <summary>
///     Defines a base class for domain events
/// </summary>
#pragma warning disable SAASDDD043
#pragma warning disable SAASDDD041
#pragma warning disable SAASDDD042
public abstract class DomainEvent : IDomainEvent
#pragma warning restore SAASDDD042
#pragma warning restore SAASDDD041
#pragma warning restore SAASDDD043
{
    protected DomainEvent()
    {
        RootId = null!;
        OccurredUtc = DateTime.UtcNow;
    }

    protected DomainEvent(string rootId)
    {
        RootId = rootId;
        OccurredUtc = DateTime.UtcNow;
    }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; }
}