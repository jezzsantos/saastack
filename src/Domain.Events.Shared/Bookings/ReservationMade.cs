using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Bookings;

#pragma warning disable SAASDDD043
public sealed class ReservationMade : IDomainEvent
#pragma warning restore SAASDDD043
{
    public required string BorrowerId { get; set; }

    public required DateTime End { get; set; }

    public required string OrganizationId { get; set; }

    public required DateTime Start { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}