using Domain.Common.ValueObjects;
using Domain.Events.Shared.Bookings;

namespace BookingsDomain;

public static class Events
{
    public static CarChanged CarChanged(Identifier id, Identifier organizationId, Identifier carId)
    {
        return new CarChanged(id)
        {
            OrganizationId = organizationId,
            CarId = carId
        };
    }

    public static Created Created(Identifier id, Identifier organizationId)
    {
        return new Created(id)
        {
            OrganizationId = organizationId
        };
    }

    public static ReservationMade ReservationMade(Identifier id, Identifier organizationId, Identifier borrowerId,
        DateTime start, DateTime end)
    {
        return new ReservationMade(id)
        {
            OrganizationId = organizationId,
            BorrowerId = borrowerId,
            Start = start,
            End = end
        };
    }

    public static TripAdded TripAdded(Identifier id, Identifier organizationId)
    {
        return new TripAdded(id)
        {
            OrganizationId = organizationId,
            TripId = null
        };
    }

    public static TripBegan TripBegan(Identifier id, Identifier organizationId, Identifier tripId,
        DateTime beganAt, Location from)
    {
        return new TripBegan(id)
        {
            OrganizationId = organizationId,
            TripId = tripId,
            BeganAt = beganAt,
            BeganFrom = from
        };
    }

    public static TripEnded TripEnded(Identifier id, Identifier organizationId, Identifier tripId, DateTime beganAt,
        Location from, DateTime endedAt, Location to)
    {
        return new TripEnded(id)
        {
            OrganizationId = organizationId,
            TripId = tripId,
            BeganAt = beganAt,
            BeganFrom = from,
            EndedAt = endedAt,
            EndedTo = to
        };
    }
}