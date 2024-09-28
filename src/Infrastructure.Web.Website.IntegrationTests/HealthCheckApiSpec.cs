using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Health;
using IntegrationTesting.WebApi.Common;
using WebsiteHost;
using Xunit;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("API")]
public class HealthCheckApiSpec : WebsiteSpec<Program>
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