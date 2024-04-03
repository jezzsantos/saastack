using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Bookings;

#pragma warning disable SAASDDD043
public sealed class TripBegan : DomainEvent
#pragma warning restore SAASDDD043
{
    public TripBegan(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TripBegan()
    {
    }

    public required DateTime BeganAt { get; set; }

    public required string BeganFrom { get; set; }

    public required string OrganizationId { get; set; }

    public required string TripId { get; set; }
}