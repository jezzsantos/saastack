using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using BookingsApplication.Persistence;
using BookingsDomain;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;

namespace BookingsApplication;

public class BookingsApplication : IBookingsApplication
{
    public static readonly TimeSpan DefaultBookingDuration = TimeSpan.FromHours(1);
    private readonly ICarsService _carsService;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IBookingRepository _repository;

    public BookingsApplication(IRecorder recorder, IIdentifierFactory idFactory, ICarsService carsService,
        IBookingRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _carsService = carsService;
        _repository = repository;
    }

    public async Task<Result<Error>> CancelBookingAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var booking = retrieved.Value;
        var cancellation = booking.Cancel();
        if (cancellation.IsFailure)
        {
            return cancellation.Error;
        }

        var released = await _carsService.ReleaseCarAvailabilityAsync(caller, organizationId, booking.CarId.Value,
            booking.Start.Value, booking.End.Value, cancellationToken);
        if (released.IsFailure)
        {
            return released.Error;
        }

        var deleted = await _repository.DeleteBookingAsync(organizationId.ToId(), booking.Id, cancellationToken);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Booking {Id} was cancelled", booking.Id);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.BookingCancelled,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.Id, booking.Id },
                { UsageConstants.Properties.Started, booking.Start.Value.Hour },
                {
                    UsageConstants.Properties.Duration,
                    booking.End.Value.Subtract(booking.Start.Value).Hours
                }
            });

        return Result.Ok;
    }

    public async Task<Result<Booking, Error>> MakeBookingAsync(ICallerContext caller, string organizationId,
        string carId, DateTime startUtc,
        DateTime? endUtc, CancellationToken cancellationToken)
    {
        var car = await _carsService.GetCarAsync(caller, organizationId, carId, cancellationToken);
        if (car.IsFailure)
        {
            return car.Error;
        }

        var bookingEndUtc = endUtc.GetValueOrDefault(startUtc.Add(DefaultBookingDuration));
        var created = BookingRoot.Create(_recorder, _idFactory, organizationId.ToId());
        if (created.IsFailure)
        {
            return created.Error;
        }

        var booking = created.Value;
        booking.ChangeCar(carId.ToId());
        booking.MakeReservation(caller.ToCallerId(), startUtc, bookingEndUtc);

        var reserved = await _carsService.ReserveCarIfAvailableAsync(caller, organizationId, carId, startUtc,
            bookingEndUtc, booking.Id, cancellationToken);
        if (reserved.IsFailure)
        {
            return reserved.Error;
        }

        if (!reserved.Value)
        {
            return Error.RuleViolation(Resources.BookingsApplication_CarNotAvailable);
        }

        var saved = await _repository.SaveAsync(booking, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        booking = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Booking {Id} was created", booking.Id);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.BookingCreated,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.Id, booking.Id },
                { UsageConstants.Properties.Started, booking.Start.Value.Hour },
                {
                    UsageConstants.Properties.Duration,
                    booking.End.Value.Subtract(booking.Start.Value).Hours
                }
            });

        return booking.ToBooking();
    }

    public async Task<Result<SearchResults<Booking>, Error>> SearchAllBookingsAsync(ICallerContext caller,
        string organizationId, DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var searched = await _repository.SearchAllBookingsAsync(organizationId.ToId(),
            fromUtc ?? DateTime.MinValue, toUtc ?? DateTime.MaxValue, searchOptions, cancellationToken);
        if (searched.IsFailure)
        {
            return searched.Error;
        }

        var bookings = searched.Value;
        _recorder.TraceInformation(caller.ToCall(), "All bookings were fetched");
        
        return searchOptions.ApplyWithMetadata(
            bookings.Select(booking => booking.ToBooking()));
    }
}

internal static class BookingConversionExtensions
{
    public static Booking ToBooking(this BookingRoot booking)
    {
        return new Booking
        {
            Id = booking.Id,
            StartUtc = booking.Start.ValueOrDefault,
            EndUtc = booking.End.ValueOrDefault,
            BorrowerId = booking.BorrowerId.ValueOrDefault!,
            CarId = booking.CarId.ValueOrDefault!
        };
    }

    public static Booking ToBooking(this Persistence.ReadModels.Booking booking)
    {
        return new Booking
        {
            Id = booking.Id.ValueOrDefault!,
            StartUtc = booking.Start.ValueOrDefault,
            EndUtc = booking.End.ValueOrDefault,
            BorrowerId = booking.BorrowerId.ValueOrDefault!,
            CarId = booking.CarId.ValueOrDefault!
        };
    }
}