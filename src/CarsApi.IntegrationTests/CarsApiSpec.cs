using System.Net;
using System.Net.Http.Json;
using ApiHost1;
using CarsApplication.Persistence;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
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
        var response = await Api.PostAsJsonAsync("/cars", new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        });

        var result = (await response.Content.ReadFromJsonAsync<GetCarResponse>())!.Car;
        var location = response.Headers.Location?.ToString();

        location.Should().StartWith("/cars/car_");
        result.Id.Should().NotBeEmpty();
        result.Make.Should().Be("amake");
        result.Model.Should().Be("amodel");
        result.Year.Should().Be(2023);
    }

    [Fact]
    public async Task WhenGetCar_ThenReturnsCar()
    {
        var response = await Api.PostAsJsonAsync("/cars", new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        });

        var car = (await response.Content.ReadFromJsonAsync<GetCarResponse>())!.Car;

        var result = await Api.GetFromJsonAsync<GetCarResponse>($"/cars/{car.Id}");

        result?.Car.Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task WhenSearchAllCars_ThenReturnsCars()
    {
        var response = await Api.PostAsJsonAsync("/cars", new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        });

        var car = (await response.Content.ReadFromJsonAsync<GetCarResponse>())!.Car;

        var result = await Api.GetFromJsonAsync<SearchAllCarsResponse>("/cars");

        result?.Cars?.Count.Should().Be(1);
        result?.Cars?[0].Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task WhenTakeCarOffline_ThenReturnsCar()
    {
        var response = await Api.PostAsJsonAsync("/cars", new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        });

        var car = (await response.Content.ReadFromJsonAsync<GetCarResponse>())!.Car;

        var result = await Api.PutAsJsonAsync($"/cars/{car.Id}/offline", new TakeOfflineCarRequest
        {
            Id = car.Id,
            StartAtUtc = DateTime.UtcNow.AddHours(1),
            EndAtUtc = DateTime.UtcNow.AddHours(2)
        });

        var offline = (await result.Content.ReadFromJsonAsync<GetCarResponse>())!.Car;

        offline.Id.Should().Be(car.Id);
    }

    [Fact]
    public async Task WhenDeleteCar_ThenDeletes()
    {
        var response = await Api.PostAsJsonAsync("/cars", new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = 2023
        });

        var car = (await response.Content.ReadFromJsonAsync<GetCarResponse>())!.Car;

        var result = await Api.DeleteAsync($"/cars/{car.Id}");

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}