using System.Net;
using ApiHost1;
using Common.FeatureFlags;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.WebApi.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AncillaryInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class FeatureFlagsApiSpec : WebApiSpec<Program>
{
    private readonly StubFeatureFlags _featureFlags;

    public FeatureFlagsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _featureFlags = setup.GetRequiredService<IFeatureFlags>().As<StubFeatureFlags>();
        _featureFlags.Reset();
    }

    [Fact]
    public async Task WhenGetAllFeatureFlags_ThenReturnsFlags()
    {
#if TESTINGONLY
        var request = new GetAllFeatureFlagsRequest();

        var result = await Api.GetAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Flags.Count.Should().Be(0);
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

        var result = await Api.GetAsync(request, req => req.SetHMACAuth(request, "asecret"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Flag.Name.Should().Be(Flag.TestingOnly.Name);
        _featureFlags.LastGetFlag.Should().Be(Flag.TestingOnly.Name);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // nothing here yet
    }
}