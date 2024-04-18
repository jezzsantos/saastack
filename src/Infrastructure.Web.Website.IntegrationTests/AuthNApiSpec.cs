using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Hosting.Common.Pipeline;
using IntegrationTesting.WebApi.Common;
using UnitTesting.Common;
using WebsiteHost;
using Xunit;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Website")]
[Collection("API")]
public class AuthNApiSpec : WebApiSpec<Program>
{
    private readonly CSRFMiddleware.ICSRFService _csrfService;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthNApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        StartupServer<ApiHost1.Program>();
        _csrfService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
        HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, _csrfService)).GetAwaiter().GetResult();
#endif
        _jsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
    }

    [Fact]
    public async Task WhenRefreshTokenAndNoCookie_ThenReturnsError()
    {
        var result = await HttpApi.PostEmptyJsonAsync(new RefreshTokenRequest().MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, _csrfService));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenRefreshTokenAndAuthenticated_ThenReturnsNewTokens()
    {
        var (userId, response) = await HttpApi.LoginUserFromBrowserAsync(_jsonOptions, _csrfService);

        var accessToken1 = response.GetCookie(AuthenticationConstants.Cookies.Token);
        var refreshToken1 = response.GetCookie(AuthenticationConstants.Cookies.RefreshToken);

        await Task.Delay(TimeSpan
            .FromSeconds(1)); //HACK: to ensure that the new token is not the same (in time) as the old token

#if TESTINGONLY
        var result = await HttpApi.PostEmptyJsonAsync(new RefreshTokenRequest().MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, _csrfService, userId));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessToken2 = result.GetCookie(AuthenticationConstants.Cookies.Token);
        var refreshToken2 = result.GetCookie(AuthenticationConstants.Cookies.RefreshToken);

        accessToken1.Should().NotBe(accessToken2);
        refreshToken1.Should().NotBe(refreshToken2);
#endif
    }

    [Fact]
    public async Task WhenAuthenticateAndWrongCredentials_ThenReturnsError()
    {
        await HttpApi.RegisterPersonUserFromBrowserAsync(_jsonOptions, _csrfService, "auser@company.com", "1Password!");

        var result =
            await HttpApi.AuthenticateUserFromBrowserAsync(_jsonOptions, _csrfService, "auser@company.com",
                "1AnotherPassword!");

        result.Response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenAuthenticateAndNotAuthenticated_ThenReturnsNewCookies()
    {
        var (_, response) = await HttpApi.LoginUserFromBrowserAsync(_jsonOptions, _csrfService);

        var accessToken = response.GetCookie(AuthenticationConstants.Cookies.Token);
        var refreshToken = response.GetCookie(AuthenticationConstants.Cookies.RefreshToken);

        accessToken.Should().NotBeNull();
        refreshToken.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenLogoutAndAuthenticated_ThenReturnsWithNoCookies()
    {
        await HttpApi.LoginUserFromBrowserAsync(_jsonOptions, _csrfService);

        var result = await HttpApi.PostEmptyJsonAsync(new LogoutRequest().MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, _csrfService));

        var accessToken = result.GetCookie(AuthenticationConstants.Cookies.Token);
        var refreshToken = result.GetCookie(AuthenticationConstants.Cookies.RefreshToken);

        accessToken.Should().BeNone();
        refreshToken.Should().BeNone();
    }

    [Fact]
    public async Task WhenAccessSecureApiAndNotAuthenticated_ThenReturnsError()
    {
#if TESTINGONLY
        var result = await HttpApi.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest().MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, _csrfService));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
#endif
    }

    [Fact]
    public async Task WhenAccessSecureApiAndAuthenticated_ThenReturnsResponse()
    {
        var (userId, _) = await HttpApi.LoginUserFromBrowserAsync(_jsonOptions, _csrfService);

#if TESTINGONLY
        var result = await HttpApi.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest().MakeApiRoute(),
            (msg, cookies) => msg.WithCSRF(cookies, _csrfService));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var callerId = (await result.Content.ReadFromJsonAsync<GetCallerTestingOnlyResponse>(_jsonOptions))!.CallerId;
        callerId.Should().Be(userId);
#endif
    }
}