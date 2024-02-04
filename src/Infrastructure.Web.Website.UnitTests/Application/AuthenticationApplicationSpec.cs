using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Interfaces.Clients;
using Microsoft.AspNetCore.Http;
using Moq;
using UnitTesting.Common;
using WebsiteHost.Application;
using Xunit;

namespace Infrastructure.Web.Website.UnitTests.Application;

[Trait("Category", "Unit")]
public class AuthenticationApplicationSpec
{
    private readonly AuthenticationApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IRequestCookieCollection> _httpRequestCookies;
    private readonly Mock<IResponseCookies> _httpResponseCookies;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IServiceClient> _serviceClient;

    public AuthenticationApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _serviceClient = new Mock<IServiceClient>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpRequest = new Mock<HttpRequest>();
        _httpRequestCookies = new Mock<IRequestCookieCollection>();
        httpRequest.Setup(req => req.Cookies).Returns(_httpRequestCookies.Object);
        var httpResponse = new Mock<HttpResponse>();
        _httpResponseCookies = new Mock<IResponseCookies>();
        httpResponse.Setup(res => res.Cookies).Returns(_httpResponseCookies.Object);
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request)
            .Returns(httpRequest.Object);
        httpContextAccessor.Setup(hca => hca.HttpContext!.Response)
            .Returns(httpResponse.Object);

        _application =
            new AuthenticationApplication(_recorder.Object, httpContextAccessor.Object, _serviceClient.Object);
    }

    [Fact]
    public async Task WhenLogout_ThenDeletesCookies()
    {
        await _application.LogoutAsync(_caller.Object, CancellationToken.None);

        _httpResponseCookies.Verify(c =>
            c.Delete(AuthenticationConstants.Cookies.Token));
        _httpResponseCookies.Verify(c =>
            c.Delete(AuthenticationConstants.Cookies.RefreshToken));
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserLogout, null));
    }

    [Fact]
    public async Task WhenAuthenticateWithCredentials_ThenAuthenticatesAndSetsCookies()
    {
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<AuthenticatePasswordRequest>(),
                null, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AuthenticateResponse, ResponseProblem>>(new AuthenticateResponse
            {
                UserId = "auserid",
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                ExpiresOnUtc = DateTime.UtcNow
            }));

        var result = await _application.AuthenticateAsync(_caller.Object, AuthenticationConstants.Providers.Credentials,
            null, "ausername", "apassword", CancellationToken.None);

        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        _serviceClient.Verify(sc => sc.PostAsync(_caller.Object, It.Is<AuthenticatePasswordRequest>(req =>
            req.Username == "ausername"
            && req.Password == "apassword"
        ), null, It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.Token, "anaccesstoken", It.IsAny<CookieOptions>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.RefreshToken, "arefreshtoken", It.IsAny<CookieOptions>()));
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserLogin, null));
    }

    [Fact]
    public async Task WhenAuthenticateWithSingleSignOn_ThenAuthenticatesAndSetsCookies()
    {
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<AuthenticateSingleSignOnRequest>(),
                null, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AuthenticateResponse, ResponseProblem>>(new AuthenticateResponse
            {
                UserId = "auserid",
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                ExpiresOnUtc = DateTime.UtcNow
            }));

        var result = await _application.AuthenticateAsync(_caller.Object,
            AuthenticationConstants.Providers.SingleSignOn,
            "anauthcode", null, null, CancellationToken.None);

        result.Value.UserId.Should().Be("auserid");
        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        _serviceClient.Verify(sc => sc.PostAsync(_caller.Object, It.Is<AuthenticateSingleSignOnRequest>(req =>
            req.AuthCode == "anauthcode"
        ), null, It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.Token, "anaccesstoken", It.IsAny<CookieOptions>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.RefreshToken, "arefreshtoken", It.IsAny<CookieOptions>()));
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserLogin, null));
    }

    [Fact]
    public async Task WhenRefreshTokenAndCookieNotExist_ThenReturnsError()
    {
        _httpRequestCookies.Setup(c => c.TryGetValue(AuthenticationConstants.Cookies.Token, out It.Ref<string?>.IsAny))
            .Returns(false);

        var result = await _application.RefreshTokenAsync(_caller.Object, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _serviceClient.Verify(
            sc => sc.PostAsync(_caller.Object, It.IsAny<RefreshTokenRequest>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), It.IsAny<string>(), null), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenCookieExists_ThenRefreshesAndSetsCookie()
    {
        _httpRequestCookies.Setup(c =>
                c.TryGetValue(AuthenticationConstants.Cookies.RefreshToken, out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? value) =>
            {
                value = "arefreshtoken";
                return true;
            });
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<RefreshTokenRequest>(),
                null, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<RefreshTokenResponse, ResponseProblem>>(new RefreshTokenResponse
            {
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                ExpiresOnUtc = DateTime.UtcNow
            }));

        var result = await _application.RefreshTokenAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.Token, "anaccesstoken", It.IsAny<CookieOptions>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.RefreshToken, "arefreshtoken", It.IsAny<CookieOptions>()));
        _serviceClient.Verify(
            sc => sc.PostAsync(_caller.Object, It.Is<RefreshTokenRequest>(req =>
                req.RefreshToken == "arefreshtoken"
            ), null, It.IsAny<CancellationToken>()));
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserExtendedLogin, null));
    }
}