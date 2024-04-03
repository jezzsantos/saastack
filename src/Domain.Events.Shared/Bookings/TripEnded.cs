using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Bookings;

public sealed class TripEnded : IDomainEvent
{
    public required DateTime BeganAt { get; set; }

    public required string BeganFrom { get; set; }

    public required DateTime EndedAt { get; set; }

    public required string EndedTo { get; set; }

    public required string OrganizationId { get; set; }

    public required string TripId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}