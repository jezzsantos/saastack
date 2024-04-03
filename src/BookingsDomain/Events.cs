using Domain.Common.ValueObjects;
using Domain.Events.Shared.Bookings;

namespace BookingsDomain;

public static class Events
{
    public static CarChanged CarChanged(Identifier id, Identifier organizationId, Identifier carId)
    {
        return new CarChanged
        {
            RootId = id,
            OrganizationId = organizationId,
            CarId = carId,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static Created Created(Identifier id, Identifier organizationId)
    {
        return new Created
        {
            RootId = id,
            OrganizationId = organizationId,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static ReservationMade ReservationMade(Identifier id, Identifier organizationId, Identifier borrowerId,
        DateTime start, DateTime end)
    {
        return new ReservationMade
        {
            RootId = id,
            OrganizationId = organizationId,
            BorrowerId = borrowerId,
            Start = start,
            End = end,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static TripBegan TripBegan(Identifier id, Identifier organizationId, Identifier tripId,
        DateTime beganAt, Location from)
    {
        return new TripBegan
        {
            RootId = id,
            OrganizationId = organizationId,
            TripId = tripId,
            BeganAt = beganAt,
            BeganFrom = from,
            OccurredUtc = DateTime.UtcNow
        };
    }

    public static TripEnded TripEnded(Identifier id, Identifier organizationId, Identifier tripId, DateTime beganAt,
        Location from, DateTime endedAt, Location to)
    {
        return new TripEnded
        {
            RootId = id,
            OrganizationId = organizationId,
            TripId = tripId,
            BeganAt = beganAt,
            BeganFrom = from,
            EndedAt = endedAt,
            EndedTo = to,
            OccurredUtc = DateTime.UtcNow
        };
    }
}