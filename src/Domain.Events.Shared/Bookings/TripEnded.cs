using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Bookings;

public sealed class TripEnded : DomainEvent
{
    public TripEnded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TripEnded()
    {
    }

    public required DateTime BeganAt { get; set; }

    public required string BeganFrom { get; set; }

    public required DateTime EndedAt { get; set; }

    public required string EndedTo { get; set; }

    public required string OrganizationId { get; set; }

    public required string TripId { get; set; }
}