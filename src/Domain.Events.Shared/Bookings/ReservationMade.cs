using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Bookings;

#pragma warning disable SAASDDD043
public sealed class ReservationMade : DomainEvent
#pragma warning restore SAASDDD043
{
    public ReservationMade(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ReservationMade()
    {
    }

    public required string BorrowerId { get; set; }

    public required DateTime End { get; set; }

    public required string OrganizationId { get; set; }

    public required DateTime Start { get; set; }
}