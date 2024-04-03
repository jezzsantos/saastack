using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Bookings;

#pragma warning disable SAASDDD043
public sealed class TripBegan : IDomainEvent
#pragma warning restore SAASDDD043
{
    public required DateTime BeganAt { get; set; }

    public required string BeganFrom { get; set; }

    public required string OrganizationId { get; set; }

    public required string TripId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}