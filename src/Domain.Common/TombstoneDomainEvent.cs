using Domain.Interfaces.Entities;
using JetBrains.Annotations;

namespace Domain.Common;

/// <summary>
///     Defines an event raised when an aggregate is deleted
/// </summary>
#pragma warning disable SAASDDD043
#pragma warning disable SAASDDD041
#pragma warning disable SAASDDD042
public abstract class TombstoneDomainEvent : DomainEvent, ITombstoneEvent
#pragma warning restore SAASDDD042
#pragma warning restore SAASDDD041
#pragma warning restore SAASDDD043
{
    protected TombstoneDomainEvent(string id, string deletedById) : base(id)
    {
        DeletedById = deletedById;
        IsTombstone = true;
    }

    [UsedImplicitly]
    protected TombstoneDomainEvent()
    {
        DeletedById = null!;
    }

    public string DeletedById { get; set; }

    public bool IsTombstone { get; set; } = true;
}