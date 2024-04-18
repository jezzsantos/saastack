using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Health;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using WebsiteHost;
using Xunit;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("API")]
public class ReverseProxyApiSpec : WebApiSpec<Program>
{
    private readonly CSRFMiddleware.ICSRFService _csrfService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ReverseProxyApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        StartupServer<ApiHost1.Program>();
        _csrfService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
        HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
                (msg, cookies) => msg.WithCSRF(cookies, _csrfService)).GetAwaiter()
            .GetResult();
#endif
        _jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
    }

    [Fact]
    public async Task WhenRequestRoot_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetStringAsync("/");

        result.Should().Contain("<html");
    }

    [Fact]
    public async Task WhenRequestAStaticFile_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetStringAsync("index.html");

        result.Should().Contain("<html");
    }

    [Fact]
    public async Task WhenRequestARegisteredWebApi_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetAsync(new HealthCheckRequest().MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var name = (await result.Content.ReadFromJsonAsync<HealthCheckResponse>(_jsonOptions))!.Name;
        name.Should().Be("WebsiteHost");
    }

    [Fact]
    public async Task WhenRequestARemoteWebApiAndNotExists_ThenReturnsNotFound()
    {
        var result = await HttpApi.GetAsync($"{WebConstants.BackEndForFrontEndBasePath}/unknown");

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenRequestAnAnonymousRemoteWebApi_ThenReverseProxies()
    {
#if TESTINGONLY
        var result = await HttpApi.GetAsync(new GetInsecureTestingOnlyRequest().MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
#endif
    }

    [Fact]
    public async Task WhenRequestASecureRemoteWebApiAndNotAuthenticated_ThenReturnsUnauthorized()
    {
#if TESTINGONLY
        var result = await HttpApi.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest().MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
#endif
    }

    [Fact]
    public async Task WhenRequestASecureRemoteWebApiAndAuthenticated_ThenReturnsResponse()
    {
        var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(_jsonOptions, _csrfService);

#if TESTINGONLY
        var result = await HttpApi.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest().MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var callerId = (await result.Content.ReadFromJsonAsync<GetCallerTestingOnlyResponse>(_jsonOptions))!.CallerId;
        callerId.Should().Be(userId);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}