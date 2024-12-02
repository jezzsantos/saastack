using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Health;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.Website.Common;
using Xunit;

namespace WebsiteHost.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("WEBSITE")]
public class HealthCheckApiSpec : WebsiteSpec<Program, ApiHost1.Program>
{
    public HealthCheckApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenCheck_ThenStatusOK()
    {
        var result = await HttpApi.GetAsync(new HealthCheckRequest().MakeApiRoute());

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{\"name\":\"WebsiteHost\",\"status\":\"OK\"}");
    }
}