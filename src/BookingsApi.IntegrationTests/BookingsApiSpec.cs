using ApiHost1;
using Application.Interfaces.Resources;
using BookingsApplication.Persistence;
using CarsDomain;
using Common.Extensions;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Operations.Bookings;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using IntegrationTesting.WebApi.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace BookingsApi.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class BookingsApiSpec : WebApiSpec<Program>
{
    public BookingsApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        var repository = setup.GetRequiredService<IBookingRepository>();
        repository.DestroyAllAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task WhenMakeBooking_ThenReturnsBooking()
    {
        var car = await RegisterNewCarAsync();
        var start = DateTime.UtcNow.ToNearestSecond().AddHours(1);
        var end = start.AddHours(1);

        var result = await Api.PostAsync(new MakeBookingRequest
        {
            CarId = car.Id,
            StartUtc = start,
            EndUtc = end
        });

        var booking = result.Content.Value.Booking!;
        var location = result.Headers.Location?.ToString();
        location.Should().BeNull();
        booking.Id.Should().NotBeEmpty();
        booking.BorrowerId.Should().Be(CallerConstants.AnonymousUserId);
        booking.CarId.Should().Be(car.Id);
        booking.StartUtc.Should().Be(start);
        booking.EndUtc.Should().Be(end);
    }

    [Fact]
    public async Task WhenSearchAllBookings_ThenReturnsBookings()
    {
        var car = await RegisterNewCarAsync();
        var start = DateTime.UtcNow.ToNearestSecond().AddHours(1);
        var end = start.AddHours(1);

        var booking = (await Api.PostAsync(new MakeBookingRequest
        {
            CarId = car.Id,
            StartUtc = start,
            EndUtc = end
        })).Content.Value.Booking!;

        var result = await Api.GetAsync(new SearchAllBookingsRequest());

        var bookings = result.Content.Value.Bookings!;
        bookings.Count.Should().Be(1);
        bookings[0].Id.Should().Be(booking.Id);
        bookings[0].BorrowerId.Should().Be(CallerConstants.AnonymousUserId);
        bookings[0].CarId.Should().Be(car.Id);
        bookings[0].StartUtc.Should().Be(start);
        bookings[0].EndUtc.Should().Be(end);
    }

    [Fact]
    public async Task WhenCancelBooking_ThenRemovesUnavailability()
    {
        var car = await RegisterNewCarAsync();
        var start = DateTime.UtcNow.ToNearestSecond().AddHours(1);
        var end = start.AddHours(1);

        var booking = (await Api.PostAsync(new MakeBookingRequest
        {
            CarId = car.Id,
            StartUtc = start,
            EndUtc = end
        })).Content.Value.Booking!;

        await Api.DeleteAsync(new CancelBookingRequest
        {
            Id = booking.Id
        });

#if TESTINGONLY
        var unavailabilities = (await Api.GetAsync(new SearchAllCarUnavailabilitiesRequest
        {
            Id = car.Id
        })).Content.Value.Unavailabilities!;

        unavailabilities.Count.Should().Be(0);
#endif
    }

    private async Task<Car> RegisterNewCarAsync()
    {
        var car = await Api.PostAsync(new RegisterCarRequest
        {
            Make = Manufacturer.AllowedMakes[0],
            Model = Manufacturer.AllowedModels[0],
            Year = 2023,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        });

        return car.Content.Value.Car!;
    }
}