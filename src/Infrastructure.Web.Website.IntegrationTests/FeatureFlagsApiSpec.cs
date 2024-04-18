using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Common.FeatureFlags;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Hosting.Common.Pipeline;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using WebsiteHost;
using Xunit;
using GetFeatureFlagResponse = Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd.GetFeatureFlagResponse;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("API")]
public class FeatureFlagsApiSpec : WebApiSpec<Program>
{
    private readonly CSRFMiddleware.ICSRFService _csrfService;
    private readonly JsonSerializerOptions _jsonOptions;

    public FeatureFlagsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        StartupServer<ApiHost1.Program>();
        StartupServer<TestingStubApiHost.Program>();
        _csrfService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
        HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
                (msg, cookies) => msg.WithCSRF(cookies, _csrfService)).GetAwaiter()
            .GetResult();
#endif
        _jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
    }

    [Fact]
    public async Task WhenGetAllFeatureFlags_ThenReturnsFlags()
    {
#if TESTINGONLY
        var request = new GetAllFeatureFlagsRequest();

        var result = await HttpApi.GetAsync(request.MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, _csrfService));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var flags = (await result.Content.ReadFromJsonAsync<GetAllFeatureFlagsResponse>(_jsonOptions))!.Flags;
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
        var flag = (await result.Content.ReadFromJsonAsync<GetFeatureFlagResponse>(_jsonOptions))!.Flag!;
        flag.Name.Should().Be(Flag.TestingOnly.Name);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}