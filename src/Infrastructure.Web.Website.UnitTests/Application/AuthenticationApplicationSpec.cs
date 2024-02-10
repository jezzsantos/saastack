using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Interfaces.Clients;
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
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IServiceClient> _serviceClient;

    public AuthenticationApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _serviceClient = new Mock<IServiceClient>();

        _application =
            new AuthenticationApplication(_recorder.Object, _serviceClient.Object);
    }

    [Fact]
    public async Task WhenLogout_ThenLogsOut()
    {
        var result = await _application.LogoutAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserLogout, null));
    }

    [Fact]
    public async Task WhenAuthenticateWithCredentials_ThenAuthenticates()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<AuthenticatePasswordRequest>(),
                null, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AuthenticateResponse, ResponseProblem>>(new AuthenticateResponse
            {
                UserId = "auserid",
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                AccessTokenExpiresOnUtc = accessTokenExpiresOn,
                RefreshTokenExpiresOnUtc = refreshTokenExpiresOn
            }));

        var result = await _application.AuthenticateAsync(_caller.Object, AuthenticationConstants.Providers.Credentials,
            null, "ausername", "apassword", CancellationToken.None);

        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.AccessTokenExpiresOn.Should().Be(accessTokenExpiresOn);
        result.Value.RefreshTokenExpiresOn.Should().Be(refreshTokenExpiresOn);
        result.Value.UserId.Should().Be("auserid");
        _serviceClient.Verify(sc => sc.PostAsync(_caller.Object, It.Is<AuthenticatePasswordRequest>(req =>
            req.Username == "ausername"
            && req.Password == "apassword"
        ), null, It.IsAny<CancellationToken>()));
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserLogin, null));
    }

    [Fact]
    public async Task WhenAuthenticateWithSingleSignOn_ThenAuthenticates()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<AuthenticateSingleSignOnRequest>(),
                null, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<AuthenticateResponse, ResponseProblem>>(new AuthenticateResponse
            {
                UserId = "auserid",
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                AccessTokenExpiresOnUtc = accessTokenExpiresOn,
                RefreshTokenExpiresOnUtc = refreshTokenExpiresOn
            }));

        var result = await _application.AuthenticateAsync(_caller.Object,
            AuthenticationConstants.Providers.SingleSignOn,
            "anauthcode", null, null, CancellationToken.None);

        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.AccessTokenExpiresOn.Should().Be(accessTokenExpiresOn);
        result.Value.RefreshTokenExpiresOn.Should().Be(refreshTokenExpiresOn);
        result.Value.UserId.Should().Be("auserid");
        _serviceClient.Verify(sc => sc.PostAsync(_caller.Object, It.Is<AuthenticateSingleSignOnRequest>(req =>
            req.AuthCode == "anauthcode"
        ), null, It.IsAny<CancellationToken>()));
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserLogin, null));
    }

    [Fact]
    public async Task WhenRefreshTokenAndCookieNotExist_ThenReturnsError()
    {
        var result = await _application.RefreshTokenAsync(_caller.Object, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _serviceClient.Verify(
            sc => sc.PostAsync(_caller.Object, It.IsAny<RefreshTokenRequest>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), It.IsAny<string>(), null), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenCookieExists_ThenRefreshesAndSetsCookie()
    {
        var accessTokenExpiresOn = DateTime.UtcNow;
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<RefreshTokenRequest>(),
                null, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<RefreshTokenResponse, ResponseProblem>>(new RefreshTokenResponse
            {
                AccessToken = "anaccesstoken",
                RefreshToken = "arefreshtoken",
                AccessTokenExpiresOnUtc = accessTokenExpiresOn,
                RefreshTokenExpiresOnUtc = refreshTokenExpiresOn
            }));

        var result = await _application.RefreshTokenAsync(_caller.Object, "arefreshtoken", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.AccessTokenExpiresOn.Should().Be(accessTokenExpiresOn);
        result.Value.RefreshTokenExpiresOn.Should().Be(refreshTokenExpiresOn);
        result.Value.UserId.Should().BeNull();
        _serviceClient.Verify(
            sc => sc.PostAsync(_caller.Object, It.Is<RefreshTokenRequest>(req =>
                req.RefreshToken == "arefreshtoken"
            ), null, It.IsAny<CancellationToken>()));
        _recorder.Verify(rec =>
            rec.TrackUsage(It.IsAny<ICallContext>(), UsageConstants.Events.UsageScenarios.UserExtendedLogin, null));
    }
}