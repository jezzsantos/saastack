using System.Net;
using System.Net.Http.Json;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Health;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Api.Operations.Shared.UserProfiles;
using Infrastructure.Web.Common;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using WebsiteHost;
using Xunit;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("API")]
public class ReverseProxyApiSpec : WebsiteSpec<Program>
{
    public ReverseProxyApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
    }

    [Fact]
    public async Task WhenRequestRoot_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetStringAsync("/");

        result.Should().Contain("<html");
    }

    [Fact]
    public async Task WhenRequestIndexHtml_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetStringAsync("index.html");

        result.Should().Contain("<html");
    }

    [Fact]
    public async Task WhenRequestAStaticFile_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetAsync("favicon.ico");

        result.Content.Headers.ContentType!.MediaType.Should().Be("image/x-icon");
        var stream = await result.Content.ReadAsStreamAsync();
        stream.Length.Should().Be(318L);
    }

    [Fact]
    public async Task WhenRequestUnknownWebPage_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetStringAsync("/apage");
        result.Should().Contain("<html");
    }

    [Fact]
    public async Task WhenRequestALocalApi_ThenDoesNotReverseProxy()
    {
        var result = await HttpApi.GetAsync(new HealthCheckRequest().MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var name = (await result.Content.ReadFromJsonAsync<HealthCheckResponse>(JsonOptions))!.Name;
        name.Should().Be(nameof(WebsiteHost));
    }

    [Fact]
    public async Task WhenRequestARemoteApi_ThenDoesReverseProxy()
    {
        var result = await HttpApi.GetAsync(new GetProfileForCallerRequest().MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await result.Content.ReadFromJsonAsync<GetProfileForCallerResponse>(JsonOptions);

        response!.Profile!.IsAuthenticated.Should().BeFalse();
        response.Profile!.Id.Should().Be(CallerConstants.AnonymousUserId);
    }

    [Fact]
    public async Task WhenRequestARemoteApiAndNotExists_ThenReturnsNotFound()
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
        var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(JsonOptions, CSRFService);

#if TESTINGONLY
        var result = await HttpApi.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest().MakeApiRoute());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var callerId = (await result.Content.ReadFromJsonAsync<GetCallerTestingOnlyResponse>(JsonOptions))!.CallerId;
        callerId.Should().Be(userId);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //do nothing
    }
}