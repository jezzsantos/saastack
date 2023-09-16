#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Infrastructure.Api.Common.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class WebApiSpec : WebApiSpecSetup<Program>
{
    public WebApiSpec(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task WhenGetApi_ThenReturns200()
    {
        var result = await Api.GetAsync("/testingonly/1");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenGetApi_ThenReturnsJsonByDefault()
    {
        var result = await Api.GetFromJsonAsync<GetTestingOnlyResponse>("/testingonly/1");

        result?.Message.Should().Be("amessage1");
    }
}
#endif