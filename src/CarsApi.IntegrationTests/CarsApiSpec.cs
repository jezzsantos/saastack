using System.Net.Http.Json;
using ApiHost1;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using IntegrationTesting.WebApi.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CarsApi.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class CarsApiSpec : WebApiSpecSetup<Program>
{
    public CarsApiSpec(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task WhenGetCar_ThenReturnsCar()
    {
        var result = await Api.GetFromJsonAsync<GetCarResponse>("/cars/car_12345678910");

        result?.Car.Id.Should().Be("car_12345678910");
    }
}