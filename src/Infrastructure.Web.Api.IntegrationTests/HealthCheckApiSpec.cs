using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Health;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class HealthCheckApiSpec : WebApiSpec<ApiHost1.Program>
{
    public HealthCheckApiSpec(WebApiSetup<ApiHost1.Program> setup) : base(setup)
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