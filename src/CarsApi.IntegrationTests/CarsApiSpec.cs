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
        var result = await Api.GetFromJsonAsync<GetCarResponse>("/cars/1234");

        result?.Car.Id.Should().Be("1234");
    }
}