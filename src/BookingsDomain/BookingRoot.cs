using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace BookingsDomain;

[EntityName("Booking")]
public sealed class BookingRoot : AggregateRootBase
{
    public static Result<BookingRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier organizationId)
    {
        var root = new BookingRoot(recorder, idFactory);
        root.RaiseCreateEvent(BookingsDomain.Events.Created.Create(root.Id, organizationId));
        return root;
    }

    private BookingRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private BookingRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(
        identifier, container, rehydratingProperties)
    {
        Start = rehydratingProperties.GetValueOrDefault<Optional<DateTime>>(nameof(Start));
        End = rehydratingProperties.GetValueOrDefault<Optional<DateTime>>(nameof(End));
        CarId = rehydratingProperties.GetValueOrDefault<Optional<Identifier>>(nameof(CarId));
        BorrowerId = rehydratingProperties.GetValueOrDefault<Optional<Identifier>>(nameof(BorrowerId));
        OrganizationId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(OrganizationId));
    }

    public Optional<Identifier> BorrowerId { get; private set; }

    private bool CanBeCancelled => Start.HasValue && Start.Value > DateTime.UtcNow;

    public Optional<Identifier> CarId { get; private set; }

    public Optional<DateTime> End { get; private set; }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    public Optional<DateTime> Start { get; private set; }

    public Trips Trips { get; } = new();

    public override HydrationProperties Dehydrate()
    {
        var properties = base.Dehydrate();
        properties.Add(nameof(Start), Start);
        properties.Add(nameof(End), End);
        properties.Add(nameof(CarId), CarId);
        properties.Add(nameof(BorrowerId), BorrowerId);
        properties.Add(nameof(OrganizationId), OrganizationId);
        return properties;
    }

    public static AggregateRootFactory<BookingRoot> Rehydrate()
    {
        return (identifier, container, properties) => new BookingRoot(identifier, container, properties);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        if (BorrowerId.Exists())
        {
            if (!CarId.HasValue)
            {
                return Error.RuleViolation(Resources.BookingRoot_ReservationRequiresCar);
            }
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Events.Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                return Result.Ok;
            }

            case Events.ReservationMade changed:
            {
                BorrowerId = changed.BorrowerId;
                Start = changed.Start;
                End = changed.End;
                return Result.Ok;
            }

            case Events.CarChanged changed:
            {
                CarId = changed.CarId.ToId();
                return Result.Ok;
            }

            case Events.TripAdded changed:
            {
                var trip = RaiseEventToChildEntity(isReconstituting, changed, idFactory =>
                    TripEntity.Create(Recorder, idFactory, RaiseChangeEvent), e => e.TripId!);
                if (!trip.IsSuccessful)
                {
                    return trip.Error;
                }

                Trips.Add(trip.Value);
                Recorder.TraceDebug(null, "Booking {Id} has created a new trip", Id);
                return Result.Ok;
            }

            case Events.TripBegan changed:
            {
                Recorder.TraceDebug(null, "Booking {Id} has started trip {TripId} from {From}",
                    Id, changed.TripId!, changed.BeganFrom);
                return Result.Ok;
            }

            case Events.TripEnded changed:
            {
                Recorder.TraceDebug(null, "Booking {Id} has ended trip {TripId} at {To}",
                    Id, changed.TripId!, changed.EndedTo);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> Cancel()
    {
        if (!CanBeCancelled)
        {
            return Error.RuleViolation(Resources.BookingRoot_BookingAlreadyStarted);
        }

        return Result.Ok;
    }

    public Result<Error> ChangeCar(Identifier carId)
    {
        return RaiseChangeEvent(BookingsDomain.Events.CarChanged.Create(Id, OrganizationId, carId));
    }

    public Result<Error> MakeReservation(Identifier borrowerId, DateTime start, DateTime end)
    {
        if (!CarId.HasValue)
        {
            return Error.RuleViolation(Resources.BookingRoot_ReservationRequiresCar);
        }

        if (end.IsInvalidParameter(e => e > start, nameof(end), Resources.BookingRoot_EndBeforeStart, out var error1))
        {
            return error1;
        }

        if (end.IsInvalidParameter(e => e.Subtract(start).Duration() >= Validations.Booking.MinimumBookingDuration,
                nameof(end), Resources.BookingRoot_BookingDurationTooShort, out var error3))
        {
            return error3;
        }

        if (end.IsInvalidParameter(e => e.Subtract(start).Duration() <= Validations.Booking.MaximumBookingDuration,
                nameof(end), Resources.BookingRoot_BookingDurationTooLong, out var error4))
        {
            return error4;
        }

        return RaiseChangeEvent(
            BookingsDomain.Events.ReservationMade.Create(Id, OrganizationId, borrowerId, start, end));
    }

    public Result<Error> StartTrip(Location from)
    {
        if (!CarId.HasValue)
        {
            return Error.RuleViolation(Resources.BookingRoot_ReservationRequiresCar);
        }

        var added = RaiseChangeEvent(BookingsDomain.Events.TripAdded.Create(Id, OrganizationId));
        if (!added.IsSuccessful)
        {
            return added.Error;
        }

        var trip = Trips.Latest()!;
        return trip.Begin(from);
    }
}