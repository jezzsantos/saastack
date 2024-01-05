using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using CarsDomain;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace CarsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class CarsApiSpec : WebApiSpec<Program>
{
    public CarsApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        EmptyAllRepositories(setup);
    }

    [Fact]
    public async Task WhenDeleteCar_ThenDeletes()
    {
        var car = await RegisterNewCarAsync();

        var result = await Api.DeleteAsync(new DeleteCarRequest { Id = car.Id });

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task WhenGetCar_ThenReturnsCar()
    {
        var car = await RegisterNewCarAsync();

        var result = (await Api.GetAsync(new GetCarRequest { Id = car.Id })).Content.Value.Car!;

        result.Id.Should().Be(car.Id);
        result.Manufacturer!.Make.Should().Be(Manufacturer.AllowedMakes[0]);
        result.Manufacturer!.Model.Should().Be(Manufacturer.AllowedModels[0]);
        result.Manufacturer!.Year.Should().Be(2023);
        result.Plate!.Jurisdiction.Should().Be(Jurisdiction.AllowedCountries[0]);
        result.Plate!.Number.Should().Be("aplate");
        result.Owner!.Id.Should().Be(CallerConstants.AnonymousUserId);
        result.Managers![0].Id.Should().Be(CallerConstants.AnonymousUserId);
        result.Status.Should().Be(CarStatus.Registered.ToString());
    }

    [Fact]
    public async Task WhenRegisterCar_ThenReturnsCar()
    {
        var result = await Api.PostAsync(new RegisterCarRequest
        {
            Make = Manufacturer.AllowedMakes[0],
            Model = Manufacturer.AllowedModels[0],
            Year = 2023,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        });

        var car = result.Content.Value.Car!;
        var location = result.Headers.Location?.ToString();
        location.Should().Be(new GetCarRequest { Id = car.Id }.ToUrl());
        car.Id.Should().NotBeEmpty();
        car.Manufacturer!.Make.Should().Be(Manufacturer.AllowedMakes[0]);
        car.Manufacturer!.Model.Should().Be(Manufacturer.AllowedModels[0]);
        car.Manufacturer!.Year.Should().Be(2023);
        car.Plate!.Jurisdiction.Should().Be(Jurisdiction.AllowedCountries[0]);
        car.Plate!.Number.Should().Be("aplate");
        car.Owner!.Id.Should().Be(CallerConstants.AnonymousUserId);
        car.Managers![0].Id.Should().Be(CallerConstants.AnonymousUserId);
        car.Status.Should().Be(CarStatus.Registered.ToString());
    }

    [Fact]
    public async Task WhenSearchAllCars_ThenReturnsCars()
    {
        var car = await RegisterNewCarAsync();

        var result = (await Api.GetAsync(new SearchAllCarsRequest())).Content.Value.Cars!;

        result.Count.Should().Be(1);
        result[0].Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task WhenSearchAvailableAndCarIsOffline_ThenReturnsAvailable()
    {
        var car1 = await RegisterNewCarAsync();
        var car2 = await RegisterNewCarAsync();
        var datum = DateTime.UtcNow.AddDays(2);
        await Api.PutAsync(new TakeOfflineCarRequest
        {
            Id = car1.Id,
            FromUtc = datum,
            ToUtc = datum.AddDays(1)
        });

        var cars = (await Api.GetAsync(new SearchAllAvailableCarsRequest
        {
            FromUtc = datum,
            ToUtc = datum.AddDays(1)
        })).Content.Value.Cars!;

        cars.Count.Should().Be(1);
        cars[0].Id.Should().Be(car2.Id);
    }

    [Fact]
    public async Task WhenTakeCarOffline_ThenReturnsCar()
    {
        var car = await RegisterNewCarAsync();
        var datum = DateTime.UtcNow.AddDays(2);

        var result = (await Api.PutAsync(new TakeOfflineCarRequest
        {
            Id = car.Id,
            FromUtc = datum,
            ToUtc = datum.AddHours(1)
        })).Content.Value.Car!;

        result.Id.Should().Be(car.Id);

#if TESTINGONLY
        var unavailabilities = (await Api.GetAsync(new SearchAllCarUnavailabilitiesRequest
        {
            Id = car.Id
        })).Content.Value.Unavailabilities!;

        unavailabilities.Count.Should().Be(1);
        unavailabilities[0].CarId.Should().Be(car.Id);
        unavailabilities[0].CausedByReason.Should().Be(UnavailabilityCausedBy.Offline.ToString());
        unavailabilities[0].CausedByReference.Should().BeNull();
#endif
    }

    [Fact]
    public async Task WhenScheduleMaintenance_ThenReturnsCar()
    {
        var car = await RegisterNewCarAsync();
        var datum = DateTime.UtcNow.AddDays(2);

        var result = (await Api.PutAsync(new ScheduleMaintenanceCarRequest
        {
            Id = car.Id,
            FromUtc = datum,
            ToUtc = datum.AddHours(1)
        })).Content.Value.Car!;

        result.Id.Should().Be(car.Id);

#if TESTINGONLY
        var unavailabilities = (await Api.GetAsync(new SearchAllCarUnavailabilitiesRequest
        {
            Id = car.Id
        })).Content.Value.Unavailabilities!;

        unavailabilities.Count.Should().Be(1);
        unavailabilities[0].CarId.Should().Be(car.Id);
        unavailabilities[0].CausedByReason.Should().Be(UnavailabilityCausedBy.Maintenance.ToString());
        unavailabilities[0].CausedByReference.Should().BeNull();
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