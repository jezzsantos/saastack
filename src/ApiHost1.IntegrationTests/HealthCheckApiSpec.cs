using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Health;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace ApiHost1.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class HealthCheckApiSpec : WebApiSpec<Program>
{
    public HealthCheckApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenCheck_ThenStatusOK()
    {
        var result = await Api.GetAsync(new HealthCheckRequest());

        result.Content.Value.Name.Should().Be("ApiHost1");
        result.Content.Value.Status.Should().Be("OK");
    }
}