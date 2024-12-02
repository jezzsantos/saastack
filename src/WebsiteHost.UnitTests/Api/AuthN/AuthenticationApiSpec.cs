using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Microsoft.AspNetCore.Http;
using Moq;
using WebsiteHost.Api.AuthN;
using WebsiteHost.Application;
using Xunit;

namespace WebsiteHost.UnitTests.Api.AuthN;

[Trait("Category", "Unit")]
public class AuthenticationApiSpec
{
    private readonly AuthenticationApi _api;
    private readonly Mock<IAuthenticationApplication> _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IRequestCookieCollection> _httpRequestCookies;
    private readonly Mock<IResponseCookies> _httpResponseCookies;

    public AuthenticationApiSpec()
    {
        _application = new Mock<IAuthenticationApplication>();
        _caller = new Mock<ICallerContext>();
        var callerFactory = new Mock<ICallerContextFactory>();
        callerFactory.Setup(ccf => ccf.Create())
            .Returns(_caller.Object);
        var httpRequest = new Mock<HttpRequest>();
        _httpRequestCookies = new Mock<IRequestCookieCollection>();
        httpRequest.Setup(req => req.Cookies).Returns(_httpRequestCookies.Object);
        var httpResponse = new Mock<HttpResponse>();
        _httpResponseCookies = new Mock<IResponseCookies>();
        httpResponse.Setup(res => res.Cookies).Returns(_httpResponseCookies.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hca => hca.HttpContext!.Request)
            .Returns(httpRequest.Object);
        httpContextAccessor.Setup(hca => hca.HttpContext!.Response)
            .Returns(httpResponse.Object);
        _api = new AuthenticationApi(callerFactory.Object, _application.Object, httpContextAccessor.Object);
    }

    [Fact]
    public async Task WhenLogout_ThenDeletesCookies()
    {
        _application.Setup(app => app.LogoutAsync(It.IsAny<ICallerContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        await _api.Logout(new LogoutRequest(), CancellationToken.None);

        _application.Verify(app => app.LogoutAsync(_caller.Object, It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(c =>
            c.Delete(AuthenticationConstants.Cookies.Token));
        _httpResponseCookies.Verify(c =>
            c.Delete(AuthenticationConstants.Cookies.RefreshToken));
    }

    [Fact]
    public async Task WhenAuthenticate_ThenSetsCookies()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _application.Setup(app => app.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    Value = "anaccesstoken",
                    ExpiresOn = accessTokenExpiresOn,
                    Type = TokenType.AccessToken
                },
                RefreshToken = new AuthenticationToken
                {
                    Value = "arefreshtoken",
                    ExpiresOn = refreshTokenExpiresOn,
                    Type = TokenType.RefreshToken
                }
            });

        await _api.Authenticate(new AuthenticateRequest
        {
            Provider = "aprovider",
            Username = "ausername",
            Password = "apassword"
        }, CancellationToken.None);

        _application.Verify(app => app.AuthenticateAsync(_caller.Object, "aprovider", null, "ausername", "apassword",
            It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.Token, "anaccesstoken", It.Is<CookieOptions>(opt =>
                opt.Expires!.Value.DateTime == accessTokenExpiresOn
            )));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.RefreshToken, "arefreshtoken", It.Is<CookieOptions>(opt =>
                opt.Expires!.Value.DateTime == refreshTokenExpiresOn
            )));
    }

    [Fact]
    public async Task WhenRefreshAndCookieNotExists_ThenReturnsError()
    {
        _httpRequestCookies.Setup(c => c.TryGetValue(AuthenticationConstants.Cookies.Token, out It.Ref<string?>.IsAny))
            .Returns(false);

        _application.Setup(app => app.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotAuthenticated());

        await _api.RefreshToken(new RefreshTokenRequest(), CancellationToken.None);

        _application.Verify(
            app => app.RefreshTokenAsync(_caller.Object, null, It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(c =>
            c.Append(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CookieOptions>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshAndCookieExists_ThenSetsCookies()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _httpRequestCookies.Setup(c =>
                c.TryGetValue(AuthenticationConstants.Cookies.RefreshToken, out It.Ref<string?>.IsAny))
            .Returns((string _, ref string? value) =>
            {
                value = "arefreshtoken";
                return true;
            });
        _application.Setup(app => app.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    Value = "anaccesstoken",
                    ExpiresOn = accessTokenExpiresOn,
                    Type = TokenType.AccessToken
                },
                RefreshToken = new AuthenticationToken
                {
                    Value = "arefreshtoken",
                    ExpiresOn = refreshTokenExpiresOn,
                    Type = TokenType.RefreshToken
                }
            });

        await _api.RefreshToken(new RefreshTokenRequest(), CancellationToken.None);

        _application.Verify(
            app => app.RefreshTokenAsync(_caller.Object, "arefreshtoken", It.IsAny<CancellationToken>()));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.Token, "anaccesstoken", It.Is<CookieOptions>(opt =>
                opt.Expires!.Value.DateTime == accessTokenExpiresOn
            )));
        _httpResponseCookies.Verify(c =>
            c.Append(AuthenticationConstants.Cookies.RefreshToken, "arefreshtoken", It.Is<CookieOptions>(opt =>
                opt.Expires!.Value.DateTime == refreshTokenExpiresOn
            )));
    }
}