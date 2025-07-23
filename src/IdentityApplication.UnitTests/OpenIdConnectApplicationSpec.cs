using Application.Interfaces;
using Application.Resources.Shared;
using Common.Configuration;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class OpenIdConnectApplicationSpec
{
    private readonly OpenIdConnectApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<IAuthTokensApplication> _authTokensApplication;
    private readonly Mock<IJWTTokensService> _jwtTokensService;

    public OpenIdConnectApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId).Returns("acallerid");

        _settings = new Mock<IConfigurationSettings>();
        _settings.Setup(s => s.Platform.GetString("BaseUrl", "https://localhost"))
            .Returns("https://localhost");

        _authTokensApplication = new Mock<IAuthTokensApplication>();
        _jwtTokensService = new Mock<IJWTTokensService>();

        _application = new OpenIdConnectApplication(
            _settings.Object,
            _authTokensApplication.Object,
            _jwtTokensService.Object);
    }

    [Fact]
    public async Task WhenGetDiscoveryDocumentAsync_ThenReturnsDiscoveryDocument()
    {
        var result = await _application.GetDiscoveryDocumentAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Issuer.Should().Be("https://localhost");
        result.Value.AuthorizationEndpoint.Should().Be("https://localhost/oauth2/authorize");
        result.Value.TokenEndpoint.Should().Be("https://localhost/oauth2/token");
        result.Value.UserInfoEndpoint.Should().Be("https://localhost/oauth2/userinfo");
        result.Value.JwksUri.Should().Be("https://localhost/.well-known/jwks.json");
        result.Value.ResponseTypesSupported.Should().Contain("code");
        result.Value.ScopesSupported.Should().Contain("openid");
        result.Value.ScopesSupported.Should().Contain("profile");
        result.Value.ScopesSupported.Should().Contain("email");
    }

    [Fact]
    public async Task WhenGetJsonWebKeySetAsync_ThenReturnsJwks()
    {
        var result = await _application.GetJsonWebKeySetAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Keys.Should().HaveCount(1);
        result.Value.Keys[0].Kty.Should().Be("oct");
        result.Value.Keys[0].Use.Should().Be("sig");
        result.Value.Keys[0].Kid.Should().Be("default");
        result.Value.Keys[0].Alg.Should().Be("HS512");
    }

    [Fact]
    public async Task WhenAuthorizeAsyncWithValidRequest_ThenReturnsAuthorizationCode()
    {
        var result = await _application.AuthorizeAsync(
            _caller.Object,
            "test-client",
            "https://example.com/callback",
            "code",
            "openid profile",
            "state123",
            "nonce456",
            null,
            null,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Code.Should().NotBeEmpty();
        result.Value.State.Should().Be("state123");
    }

    [Fact]
    public async Task WhenAuthorizeAsyncWithInvalidResponseType_ThenReturnsError()
    {
        var result = await _application.AuthorizeAsync(
            _caller.Object,
            "test-client",
            "https://example.com/callback",
            "token", // Invalid response type
            "openid profile",
            null,
            null,
            null,
            null,
            CancellationToken.None);

        result.Should().BeError();
        result.Error.Message.Should().Contain("Only authorization code flow is supported");
    }

    [Fact]
    public async Task WhenAuthorizeAsyncWithoutOpenIdScope_ThenReturnsError()
    {
        var result = await _application.AuthorizeAsync(
            _caller.Object,
            "test-client",
            "https://example.com/callback",
            "code",
            "profile email", // Missing openid scope
            null,
            null,
            null,
            null,
            CancellationToken.None);

        result.Should().BeError();
        result.Error.Message.Should().Contain("OpenID scope is required");
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsync_ThenReturnsTokens()
    {
        var mockTokens = new AccessTokens
        {
            AccessToken = new AuthenticationToken
            {
                Value = "access-token",
                ExpiresOn = DateTime.UtcNow.AddMinutes(15),
                Type = TokenType.AccessToken
            },
            RefreshToken = new AuthenticationToken
            {
                Value = "refresh-token",
                ExpiresOn = DateTime.UtcNow.AddDays(14),
                Type = TokenType.RefreshToken
            }
        };

        _authTokensApplication.Setup(a => a.IssueTokensAsync(
                It.IsAny<ICallerContext>(),
                It.IsAny<EndUserWithMemberships>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTokens);

        var result = await _application.ExchangeCodeForTokensAsync(
            _caller.Object,
            "test-client",
            "test-secret",
            "auth-code",
            "https://example.com/callback",
            null,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.IdToken.Should().NotBeNull();
        result.Value.Scope.Should().Be("openid profile email");
    }

    [Fact]
    public async Task WhenRefreshTokenAsync_ThenReturnsNewTokens()
    {
        var mockTokens = new AccessTokens
        {
            AccessToken = new AuthenticationToken
            {
                Value = "new-access-token",
                ExpiresOn = DateTime.UtcNow.AddMinutes(15),
                Type = TokenType.AccessToken
            },
            RefreshToken = new AuthenticationToken
            {
                Value = "new-refresh-token",
                ExpiresOn = DateTime.UtcNow.AddDays(14),
                Type = TokenType.RefreshToken
            }
        };

        _authTokensApplication.Setup(a => a.RefreshTokenAsync(
                It.IsAny<ICallerContext>(),
                "old-refresh-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTokens);

        var result = await _application.RefreshTokenAsync(
            _caller.Object,
            "test-client",
            "test-secret",
            "old-refresh-token",
            "openid profile",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        result.Value.Scope.Should().Be("openid profile");
    }
}