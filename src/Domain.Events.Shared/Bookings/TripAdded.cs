using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Bookings;

public sealed class TripAdded : DomainEvent
{
    public TripAdded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public TripAdded()
    {
    }

    public required string OrganizationId { get; set; }

    public string? TripId { get; set; }
}