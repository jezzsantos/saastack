using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using BookingsApplication.Persistence;
using BookingsDomain;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using Booking = BookingsApplication.Persistence.ReadModels.Booking;

namespace BookingsApplication.UnitTests;

[Trait("Category", "Unit")]
public class BookingsApplicationSpec
{
    private readonly BookingsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<ICarsService> _carsService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IBookingRepository> _repository;

    public BookingsApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _repository = new Mock<IBookingRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<BookingRoot>(), It.IsAny<CancellationToken>()))
            .Returns((BookingRoot car, CancellationToken _) => Task.FromResult<Result<BookingRoot, Error>>(car));
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId).Returns("acallerid");
        _carsService = new Mock<ICarsService>();
        _application = new BookingsApplication(_recorder.Object, _idFactory.Object,
            _carsService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenCancelBookingAsyncWithUnknownBooking_ThenReturnsError()
    {
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<BookingRoot, Error>>(Error.EntityNotFound()));

        var result =
            await _application.CancelBookingAsync(_caller.Object, "anorganizationid", "anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _repository.Verify(
            rep => rep.DeleteBookingAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenCancelBookingAsyncAndNotReserved_ThenReturnsError()
    {
        var booking = BookingRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        booking.Value.ChangeCar("acarid".ToId());
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(1);
        booking.Value.MakeReservation("aborrowerid".ToId(), start, end);
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(booking));
        _carsService.Setup(cs => cs.ReleaseCarAvailabilityAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Car, Error>>(Error.RuleViolation("notreserved")));

        var result =
            await _application.CancelBookingAsync(_caller.Object, "anorganizationid", "anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, "notreserved");
        _carsService.Verify(cs => cs.ReleaseCarAvailabilityAsync(_caller.Object, "anorganizationid", "acarid", start,
            end, It.IsAny<CancellationToken>()));
        _repository.Verify(
            rep => rep.DeleteBookingAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenCancelBookingAsyncAndReserved_ThenDeletes()
    {
        var booking = BookingRoot.Create(_recorder.Object, _idFactory.Object,
            "anorganizationid".ToId());
        booking.Value.ChangeCar("acarid".ToId());
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(1);
        booking.Value.MakeReservation("aborrowerid".ToId(), start, end);
        _repository.Setup(s =>
                s.LoadAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(booking));
        _carsService.Setup(cs => cs.ReleaseCarAvailabilityAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Car, Error>>(new Car
            {
                Id = "acarid",
                Managers = new List<CarManager>(),
                Manufacturer = new CarManufacturer
                {
                    Make = "amake",
                    Model = "amodel",
                    Year = 2023
                },
                Owner = new CarOwner { Id = "anownerid" },
                Plate = new CarLicensePlate { Jurisdiction = "ajurisdiction", Number = "anumber" },
                Status = "astatus"
            }));

        var result =
            await _application.CancelBookingAsync(_caller.Object, "anorganizationid", "anid", CancellationToken.None);

        result.Should().Be(Result.Ok);
        _carsService.Verify(cs => cs.ReleaseCarAvailabilityAsync(_caller.Object, "anorganizationid", "acarid", start,
            end, It.IsAny<CancellationToken>()));
        _repository.Verify(rep =>
            rep.DeleteBookingAsync("anorganizationid".ToId(), "anid".ToId(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenMakeBookingAsyncWithUnknownCar_ThenReturnsError()
    {
        _carsService.Setup(cs => cs.GetCarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Car, Error>>(Error.EntityNotFound()));

        var result =
            await _application.MakeBookingAsync(_caller.Object, "anorganizationid", "anid", DateTime.UtcNow,
                DateTime.UtcNow, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _carsService.Verify(
            cs => cs.ReserveCarIfAvailableAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenMakeBookingAsyncAndHasNoAvailability_ThenReturnsError()
    {
        _carsService.Setup(cs => cs.GetCarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Car, Error>>(new Car
            {
                Id = "acarid",
                Managers = null,
                Manufacturer = null,
                Owner = null,
                Plate = null,
                Status = "astatus"
            }));
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        _carsService.Setup(
                cs => cs.ReserveCarIfAvailableAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var result =
            await _application.MakeBookingAsync(_caller.Object, "anorganizationid", "acarid", start,
                end, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BookingsApplication_CarNotAvailable);
        _carsService.Verify(
            cs => cs.ReserveCarIfAvailableAsync(_caller.Object
                , "anorganizationid", "acarid",
                start, end, "anid", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<BookingRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenMakeBookingAsyncAndHasAvailability_ThenReturnsBooking()
    {
        _carsService.Setup(cs => cs.GetCarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Car, Error>>(new Car
            {
                Id = "acarid",
                Managers = null,
                Manufacturer = null,
                Owner = null,
                Plate = null,
                Status = "astatus"
            }));
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        _carsService.Setup(
                cs => cs.ReserveCarIfAvailableAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(true));

        var result =
            await _application.MakeBookingAsync(_caller.Object, "anorganizationid", "acarid", start,
                end, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.CarId.Should().Be("acarid");
        result.Value.BorrowerId.Should().Be("acallerid");
        result.Value.EndUtc.Should().Be(end);
        result.Value.StartUtc.Should().Be(start);
        _carsService.Verify(
            cs => cs.ReserveCarIfAvailableAsync(_caller.Object
                , "anorganizationid", "acarid",
                start, end, "anid", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<BookingRoot>(booking =>
            booking.Id == "anid"
            && booking.OrganizationId == "anorganizationid"
            && booking.CarId == "acarid".ToId()
            && booking.BorrowerId == "acallerid".ToId()
            && booking.Start == start
            && booking.End == end
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenMakeBookingAsyncAndHasNoEndDateAndHasAvailability_ThenReturnsBooking()
    {
        _carsService.Setup(cs => cs.GetCarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Car, Error>>(new Car
            {
                Id = "acarid",
                Managers = null,
                Manufacturer = null,
                Owner = null,
                Plate = null,
                Status = "astatus"
            }));
        var start = DateTime.UtcNow;
        _carsService.Setup(
                cs => cs.ReserveCarIfAvailableAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(true));

        var result =
            await _application.MakeBookingAsync(_caller.Object, "anorganizationid", "acarid", start, null,
                CancellationToken.None);

        var expectedEnd = start.Add(BookingsApplication.DefaultBookingDuration);
        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.CarId.Should().Be("acarid");
        result.Value.BorrowerId.Should().Be("acallerid");
        result.Value.EndUtc.Should().Be(expectedEnd);
        result.Value.StartUtc.Should().Be(start);
        _carsService.Verify(
            cs => cs.ReserveCarIfAvailableAsync(_caller.Object
                , "anorganizationid", "acarid",
                start, expectedEnd, "anid", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<BookingRoot>(booking =>
            booking.Id == "anid"
            && booking.OrganizationId == "anorganizationid"
            && booking.CarId == "acarid".ToId()
            && booking.BorrowerId == "acallerid".ToId()
            && booking.Start == start
            && booking.End == expectedEnd
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllBookingsAsync_ThenReturnsBookings()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        _repository.Setup(rep => rep.SearchAllBookingsAsync(It.IsAny<Identifier>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<IReadOnlyList<Booking>, Error>>(new List<Booking>
            {
                new()
                {
                    Id = "abookingid",
                    BorrowerId = "aborrowerid",
                    CarId = "acarid",
                    End = end,
                    OrganizationId = "anorganizationid",
                    Start = start
                }
            }));

        var result = await _application.SearchAllBookingsAsync(_caller.Object, "anorganizationid", start, end,
            new SearchOptions(), new GetOptions(), CancellationToken.None);

        var bookings = result.Value.Results;
        bookings.Count.Should().Be(1);
        bookings[0].Id.Should().Be("abookingid");
        bookings[0].BorrowerId.Should().Be("aborrowerid");
        bookings[0].CarId.Should().Be("acarid");
        bookings[0].StartUtc.Should().Be(start);
        bookings[0].EndUtc.Should().Be(end);
    }
}