using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Bookings;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace BookingsDomain.UnitTests;

[Trait("Category", "Unit")]
public class BookingRootSpec
{
    private readonly BookingRoot _booking;

    public BookingRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity _) => "anid".ToId());
        _booking = BookingRoot.Create(recorder.Object, identifierFactory.Object,
            "anorganizationid".ToId()).Value;
    }

    [Fact]
    public void WhenCreate_ThenNotReserved()
    {
        _booking.OrganizationId.Should().Be("anorganizationid".ToId());
        _booking.CarId.Should().BeNone();
        _booking.BorrowerId.Should().BeNone();
        _booking.Start.Should().BeNone();
        _booking.End.Should().BeNone();
    }

    [Fact]
    public void WhenChangeCar_ThenAssigned()
    {
        _booking.ChangeCar("acarid".ToId());

        _booking.CarId.Should().Be("acarid".ToId());
        _booking.Events.Last().Should().BeOfType<CarChanged>();
    }

    [Fact]
    public void WhenMakeReservationAndNoCar_ThenReturnsError()
    {
        var start = DateTime.UtcNow.ToNearestSecond();
        var end = start.AddHours(1);

        var result = _booking.MakeReservation("aborrowerid".ToId(), start, end);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BookingRoot_ReservationRequiresCar);
    }

    [Fact]
    public void WhenMakeReservationAndEndBeforeStart_ThenReturnsError()
    {
        var start = DateTime.UtcNow.ToNearestSecond();
        var end = start.SubtractHours(1);
        _booking.ChangeCar("acarid".ToId());

        var result = _booking.MakeReservation("aborrowerid".ToId(), start, end);

        result.Should().BeError(ErrorCode.Validation, Resources.BookingRoot_EndBeforeStart);
    }

    [Fact]
    public void WhenMakeReservationDurationTooShort_ThenReturnsError()
    {
        var start = DateTime.UtcNow.ToNearestSecond();
        var end = start.Add(Validations.Booking.MinimumBookingDuration).SubtractSeconds(1);
        _booking.ChangeCar("acarid".ToId());

        var result = _booking.MakeReservation("aborrowerid".ToId(), start, end);

        result.Should().BeError(ErrorCode.Validation, Resources.BookingRoot_BookingDurationTooShort);
    }

    [Fact]
    public void WhenMakeReservationDurationTooLong_ThenReturnsError()
    {
        var start = DateTime.UtcNow.ToNearestSecond();
        var end = start.Add(Validations.Booking.MaximumBookingDuration).AddSeconds(1);
        _booking.ChangeCar("acarid".ToId());

        var result = _booking.MakeReservation("aborrowerid".ToId(), start, end);

        result.Should().BeError(ErrorCode.Validation, Resources.BookingRoot_BookingDurationTooLong);
    }

    [Fact]
    public void WhenMakeReservation_ThenAssigned()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        _booking.ChangeCar("acarid".ToId());

        _booking.MakeReservation("aborrowerid".ToId(), start, end);

        _booking.CarId.Should().Be("acarid".ToId());
        _booking.BorrowerId.Should().Be("aborrowerid".ToId());
        _booking.Start.Should().Be(start);
        _booking.End.Should().Be(end);
        _booking.Events.Last().Should().BeOfType<ReservationMade>();
    }

    [Fact]
    public void WhenStartTripAndNoCar_ThenReturnsError()
    {
        var result = _booking.StartTrip(Location.Create("alocation").Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.BookingRoot_ReservationRequiresCar);
    }

    [Fact]
    public void WhenStartTrip_ThenAddsTrip()
    {
        _booking.ChangeCar("acarid".ToId());

        var result = _booking.StartTrip(Location.Create("alocation").Value);

        result.Should().BeSuccess();
        _booking.Trips.Count().Should().Be(1);
        _booking.Events.Last().Should().BeOfType<TripBegan>();
    }
}