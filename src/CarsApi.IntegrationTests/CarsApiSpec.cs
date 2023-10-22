using System.Net;
using ApiHost1;
using CarsApplication.Persistence;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace CarsApi.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class CarsApiSpec : WebApiSpec<Program>
{
    public CarsApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        var repository = setup.GetRequiredService<ICarRepository>();
        repository.DestroyAll();
    }

    [Fact]
    public async Task WhenRegisterCar_ThenReturnsCar()
    {
        var result = await Api.PostAsync(new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        });

        var location = result.Headers.Location?.ToString();

        location.Should().StartWith("/cars/car_");
        result.Content.Value.Car!.Id.Should().NotBeEmpty();
        result.Content.Value.Car.Make.Should().Be("amake");
        result.Content.Value.Car.Model.Should().Be("amodel");
        result.Content.Value.Car.Year.Should().Be(2023);
    }

    [Fact]
    public async Task WhenGetCar_ThenReturnsCar()
    {
        var car = (await Api.PostAsync(new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        })).Content.Value.Car!;

        var result = (await Api.GetAsync(new GetCarRequest { Id = car.Id })).Content.Value.Car!;

        result.Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task WhenSearchAllCars_ThenReturnsCars()
    {
        var car = (await Api.PostAsync(new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        })).Content.Value.Car!;

        var result = (await Api.GetAsync(new SearchAllCarsRequest())).Content.Value.Cars!;

        result.Count.Should().Be(1);
        result[0]
            .Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task WhenTakeCarOffline_ThenReturnsCar()
    {
        var car = (await Api.PostAsync(new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        })).Content.Value.Car!;

        var result = (await Api.PutAsync(new TakeOfflineCarRequest
        {
            Id = car.Id,
            StartAtUtc = DateTime.UtcNow.AddHours(1),
            EndAtUtc = DateTime.UtcNow.AddHours(2)
        })).Content.Value.Car!;

        result.Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task WhenDeleteCar_ThenDeletes()
    {
        var car = (await Api.PostAsync(new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        })).Content.Value.Car!;

        var result = await Api.DeleteAsync(new DeleteCarRequest { Id = car.Id });

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}