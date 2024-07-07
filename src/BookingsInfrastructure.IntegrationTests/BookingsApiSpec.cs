using ApiHost1;
using Application.Resources.Shared;
using CarsDomain;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Bookings;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace BookingsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class BookingsApiSpec : WebApiSpec<Program>
{
    public BookingsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenMakeBooking_ThenReturnsBooking()
    {
        var login = await LoginUserAsync();
        var car = await RegisterNewCarAsync(login);
        var start = DateTime.UtcNow.ToNearestSecond().AddHours(1);
        var end = start.AddHours(1);

        var result = await Api.PostAsync(new MakeBookingRequest
        {
            CarId = car.Id,
            StartUtc = start,
            EndUtc = end
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var booking = result.Content.Value.Booking!;
        var location = result.Headers.Location?.ToString();
        location.Should().BeNull();
        booking.Id.Should().NotBeEmpty();
        booking.BorrowerId.Should().Be(login.User.Id);
        booking.CarId.Should().Be(car.Id);
        booking.StartUtc.Should().Be(start);
        booking.EndUtc.Should().Be(end);
    }

    [Fact]
    public async Task WhenSearchAllBookings_ThenReturnsBookings()
    {
        var login = await LoginUserAsync();
        var car = await RegisterNewCarAsync(login);
        var start = DateTime.UtcNow.ToNearestSecond().AddHours(1);
        var end = start.AddHours(1);

        var booking = (await Api.PostAsync(new MakeBookingRequest
        {
            CarId = car.Id,
            StartUtc = start,
            EndUtc = end
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Booking!;

        var result =
            await Api.GetAsync(new SearchAllBookingsRequest(), req => req.SetJWTBearerToken(login.AccessToken));

        var bookings = result.Content.Value.Bookings!;
        bookings.Count.Should().Be(1);
        bookings[0].Id.Should().Be(booking.Id);
        bookings[0].BorrowerId.Should().Be(login.User.Id);
        bookings[0].CarId.Should().Be(car.Id);
        bookings[0].StartUtc.Should().Be(start);
        bookings[0].EndUtc.Should().Be(end);
    }

    [Fact]
    public async Task WhenCancelBooking_ThenRemovesUnavailability()
    {
        var login = await LoginUserAsync();
        var car = await RegisterNewCarAsync(login);
        var start = DateTime.UtcNow.ToNearestSecond().AddHours(1);
        var end = start.AddHours(1);

        var booking = (await Api.PostAsync(new MakeBookingRequest
        {
            CarId = car.Id,
            StartUtc = start,
            EndUtc = end
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Booking!;

        await Api.DeleteAsync(new CancelBookingRequest
        {
            Id = booking.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

#if TESTINGONLY
        var unavailabilities = (await Api.GetAsync(new SearchAllCarUnavailabilitiesRequest
        {
            Id = car.Id
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Unavailabilities!;

        unavailabilities.Count.Should().Be(0);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // do nothing
    }

    private async Task<Car> RegisterNewCarAsync(LoginDetails login)
    {
        var car = await Api.PostAsync(new RegisterCarRequest
        {
            Make = Manufacturer.AllowedMakes[0],
            Model = Manufacturer.AllowedModels[0],
            Year = 2023,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        return car.Content.Value.Car!;
    }
}