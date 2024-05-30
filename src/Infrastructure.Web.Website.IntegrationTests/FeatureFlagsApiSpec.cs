using System.Net;
using System.Net.Http.Json;
using Common.FeatureFlags;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using WebsiteHost;
using Xunit;
using GetFeatureFlagResponse = Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.GetFeatureFlagResponse;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("API")]
public class FeatureFlagsApiSpec : WebsiteSpec<Program>
{
    public FeatureFlagsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        StartupServer<TestingStubApiHost.Program>();
    }

    [Fact]
    public async Task WhenGetAllFeatureFlags_ThenReturnsFlags()
    {
#if TESTINGONLY
        var request = new GetAllFeatureFlagsRequest();

        var result = await HttpApi.GetAsync(request.MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, CSRFService));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var flags = (await result.Content.ReadFromJsonAsync<GetAllFeatureFlagsResponse>(JsonOptions))!.Flags;
        flags.Count.Should().Be(2);
        flags[0].Name.Should().Be(Flag.TestingOnly.Name);
        flags[1].Name.Should().Be(Flag.AFeatureFlag.Name);
#endif
    }

    [Fact]
    public async Task WhenGetFeatureFlag_ThenReturnsFlag()
    {
#if TESTINGONLY
        var request = new GetFeatureFlagForCallerRequest
        {
            Name = Flag.TestingOnly.Name
        };

        var result = await HttpApi.GetAsync(request.MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var flag = (await result.Content.ReadFromJsonAsync<GetFeatureFlagResponse>(JsonOptions))!.Flag!;
        flag.Name.Should().Be(Flag.TestingOnly.Name);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //do nothing
    }
}