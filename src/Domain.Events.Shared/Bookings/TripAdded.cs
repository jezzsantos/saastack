using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Domain.Events.Shared.Bookings;

public sealed class TripAdded : IDomainEvent
{
    public static TripAdded Create(Identifier id, Identifier organizationId)
    {
        return new TripAdded
        {
            RootId = id,
            OrganizationId = organizationId,
            TripId = null,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public required string OrganizationId { get; set; }

    public string? TripId { get; set; }

    public required string RootId { get; set; }

    public required DateTime OccurredUtc { get; set; }
}