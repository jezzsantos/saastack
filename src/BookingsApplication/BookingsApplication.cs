using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Resources;
using Application.Interfaces.Services;
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
        var booking = await _repository.LoadAsync(organizationId.ToId(), id.ToId(), cancellationToken);
        if (!booking.IsSuccessful)
        {
            return booking.Error;
        }

        var cancellation = booking.Value.Cancel();
        if (!cancellation.IsSuccessful)
        {
            return cancellation.Error;
        }

        var released = await _carsService.ReleaseCarAvailabilityAsync(caller, organizationId, booking.Value.CarId.Value,
            booking.Value.Start.Value, booking.Value.End.Value, cancellationToken);
        if (!released.IsSuccessful)
        {
            return released.Error;
        }

        var deleted = await _repository.DeleteBookingAsync(organizationId.ToId(), booking.Value.Id, cancellationToken);
        if (!deleted.IsSuccessful)
        {
            return deleted.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Booking {Id} was cancelled", booking.Value.Id);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.BookingCancelled,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.Id, booking.Value.Id },
                { UsageConstants.Properties.Started, booking.Value.Start.Value.Hour },
                {
                    UsageConstants.Properties.Duration,
                    booking.Value.End.Value.Subtract(booking.Value.Start.Value).Hours
                }
            });

        return Result.Ok;
    }

    public async Task<Result<Booking, Error>> MakeBookingAsync(ICallerContext caller, string organizationId,
        string carId, DateTime startUtc,
        DateTime? endUtc, CancellationToken cancellationToken)
    {
        var car = await _carsService.GetCarAsync(caller, organizationId, carId, cancellationToken);
        if (!car.IsSuccessful)
        {
            return car.Error;
        }

        var bookingEndUtc = endUtc.GetValueOrDefault(startUtc.Add(DefaultBookingDuration));
        var booking = BookingRoot.Create(_recorder, _idFactory, organizationId.ToId());
        if (!booking.IsSuccessful)
        {
            return booking.Error;
        }

        booking.Value.ChangeCar(carId.ToId());
        booking.Value.MakeReservation(caller.ToCallerId(), startUtc, bookingEndUtc);

        var reserved = await _carsService.ReserveCarIfAvailableAsync(caller, organizationId, carId, startUtc,
            bookingEndUtc, booking.Value.Id, cancellationToken);
        if (!reserved.IsSuccessful)
        {
            return reserved.Error;
        }

        if (!reserved.Value)
        {
            return Error.RuleViolation(Resources.BookingsApplication_CarNotAvailable);
        }

        var created = await _repository.SaveAsync(booking.Value, cancellationToken);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Booking {Id} was created", created.Value.Id);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.BookingCreated,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.Id, created.Value.Id },
                { UsageConstants.Properties.Started, created.Value.Start.Value.Hour },
                {
                    UsageConstants.Properties.Duration,
                    created.Value.End.Value.Subtract(created.Value.Start.Value).Hours
                }
            });

        return created.Value.ToBooking();
    }

    public async Task<Result<SearchResults<Booking>, Error>> SearchAllBookingsAsync(ICallerContext caller,
        string organizationId, DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken)
    {
        var bookings = await _repository.SearchAllBookingsAsync(organizationId.ToId(),
            fromUtc ?? DateTime.MinValue, toUtc ?? DateTime.MaxValue, searchOptions, cancellationToken);
        if (!bookings.IsSuccessful)
        {
            return bookings.Error;
        }

        return searchOptions.ApplyWithMetadata(
            bookings.Value.Select(booking => booking.ToBooking()));
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
            BorrowerId = booking.BorrowerId.HasValue
                ? booking.BorrowerId.Value.Text
                : null,
            CarId = booking.CarId.HasValue
                ? booking.CarId.Value.Text
                : null
        };
    }

    public static Booking ToBooking(this Persistence.ReadModels.Booking booking)
    {
        return new Booking
        {
            Id = booking.Id.ValueOrDefault!,
            StartUtc = booking.Start.ValueOrDefault,
            EndUtc = booking.End.ValueOrDefault,
            BorrowerId = booking.BorrowerId.ValueOrDefault,
            CarId = booking.CarId.ValueOrDefault
        };
    }
}