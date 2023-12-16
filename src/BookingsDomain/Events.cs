using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace BookingsDomain;

public static class Events
{
    public class Created : IDomainEvent
    {
        public static Created Create(Identifier id, Identifier organizationId)
        {
            return new Created
            {
                RootId = id,
                OrganizationId = organizationId,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public required string OrganizationId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public class ReservationMade : IDomainEvent
    {
        public static ReservationMade Create(Identifier id, Identifier organizationId, Identifier borrowerId,
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

        public required Identifier BorrowerId { get; set; }

        public required DateTime End { get; set; }

        public required string OrganizationId { get; set; }

        public required DateTime Start { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public class CarChanged : IDomainEvent
    {
        public static CarChanged Create(Identifier id, Identifier organizationId, Identifier carId)
        {
            return new CarChanged
            {
                RootId = id,
                OrganizationId = organizationId,
                CarId = carId,
                OccurredUtc = DateTime.UtcNow
            };
        }

        public required string CarId { get; set; }

        public required string OrganizationId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public class TripAdded : IDomainEvent
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

    public class TripBegan : IDomainEvent
    {
        public static TripBegan Create(Identifier id, Identifier organizationId, Identifier tripId,
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

        public required DateTime BeganAt { get; set; }

        public required string BeganFrom { get; set; }

        public required string OrganizationId { get; set; }

        public string? TripId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }

    public class TripEnded : IDomainEvent
    {
        public static TripEnded Create(Identifier id, Identifier organizationId, Identifier tripId, DateTime beganAt,
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

        public required DateTime BeganAt { get; set; }

        public required string BeganFrom { get; set; }

        public required DateTime EndedAt { get; set; }

        public required string EndedTo { get; set; }

        public required string OrganizationId { get; set; }

        public string? TripId { get; set; }

        public required string RootId { get; set; }

        public required DateTime OccurredUtc { get; set; }
    }
}