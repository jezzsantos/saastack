using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using OAuth2GrantType = Application.Resources.Shared.OAuth2GrantType;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;
using OAuth2TokenType = Application.Resources.Shared.OAuth2TokenType;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class OpenIdConnectApplicationSpec
{
    private readonly OpenIdConnectApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentityServerOpenIdConnectService> _oidcService;

    public OpenIdConnectApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId)
            .Returns("acallerid");

        _oidcService = new Mock<IIdentityServerOpenIdConnectService>();
        var identityServerProvider = new Mock<IIdentityServerProvider>();
        identityServerProvider.Setup(p => p.OpenIdConnectService)
            .Returns(_oidcService.Object);

        _application = new OpenIdConnectApplication(identityServerProvider.Object);
    }

    [Fact]
    public async Task WhenGetDiscoveryDocumentAsync_ThenReturnsDiscoveryDocument()
    {
        var document = new OpenIdConnectDiscoveryDocument
        {
            Issuer = "anissuer",
            AuthorizationEndpoint = "",
            TokenEndpoint = "",
            UserInfoEndpoint = "",
            JwksUri = "",
            ResponseTypesSupported = [],
            ScopesSupported = [],
            SubjectTypesSupported = [],
            IdTokenSigningAlgValuesSupported = [],
            TokenEndpointAuthMethodsSupported = [],
            ClaimsSupported = [],
            CodeChallengeMethodsSupported = [],
            TokenEndpointAuthSigningAlgValuesSupported = [],
            UserInfoSigningAlgValuesSupported = [],
            RegistrationEndPoint = "",
            UserInfoEncryptionAlgValuesSupported = [],
            IdTokenEncryptionAlgValuesSupported = []
        };

        _oidcService.Setup(s => s.GetDiscoveryDocumentAsync(_caller.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var result = await _application.GetDiscoveryDocumentAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().Be(document);
    }

    [Fact]
    public async Task WhenGetJsonWebKeySetAsync_ThenReturnsJwks()
    {
        var jwks = new JsonWebKeySet
        {
            Keys =
            [
                new JsonWebKey
                {
                    Kty = "akeytype",
                    Use = "akeyuse",
                    Kid = "akeyid",
                    Alg = OAuth2Constants.SigningAlgorithms.Hs512
                }
            ]
        };

        _oidcService.Setup(s => s.GetJsonWebKeySetAsync(_caller.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwks);

        var result = await _application.GetJsonWebKeySetAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().Be(jwks);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncWith_ThenReturnsAuthorizationCode()
    {
        _oidcService.Setup(s => s.AuthorizeAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OAuth2ResponseType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OpenIdConnectCodeChallengeMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenIdConnectAuthorization
            {
                Code = new OpenIdConnectAuthorizationCode
                {
                    Code = "anauthorizationcode",
                    State = "astate"
                }
            });

        var result = await _application.AuthorizeAsync(_caller.Object, "aclientid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", "acodechallenge",
            OpenIdConnectCodeChallengeMethod.Plain,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Code!.Code.Should().Be("anauthorizationcode");
        result.Value.Code.State.Should().Be("astate");
        _oidcService.Verify(s => s.AuthorizeAsync(_caller.Object, "aclientid", "acallerid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", "acodechallenge",
            OpenIdConnectCodeChallengeMethod.Plain,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncWithAuthorizationCode_ThenReturnsTokens()
    {
        _oidcService.Setup(s => s.ExchangeCodeForTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenIdConnectTokens
            {
                AccessToken = "anaccesstoken",
                TokenType = OAuth2TokenType.Bearer,
                ExpiresIn = 1,
                RefreshToken = "arefreshtoken",
                IdToken = "anidtoken"
            });

        var result = await _application.ExchangeCodeForTokensAsync(_caller.Object, OAuth2GrantType.Authorization_Code,
            "aclientid", "aclientsecret", "acode", "aredirecturi", "acodeverifier", "arefreshtoken",
            "ascope", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
        result.Value.ExpiresIn.Should().Be(1);
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.IdToken.Should().Be("anidtoken");
        _oidcService.Verify(s => s.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret", "acode",
            "aredirecturi", "acodeverifier", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncWithRefreshToken_ThenReturnsNewTokens()
    {
        _oidcService.Setup(s => s.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenIdConnectTokens
            {
                AccessToken = "anaccesstoken",
                TokenType = OAuth2TokenType.Bearer,
                ExpiresIn = 1,
                RefreshToken = "arefreshtoken",
                IdToken = "anidtoken"
            });

        var result = await _application.ExchangeCodeForTokensAsync(_caller.Object, OAuth2GrantType.Refresh_Token,
            "aclientid", "aclientsecret", "acode", "aredirecturi", "acodeverifier", "arefreshtoken",
            "ascope", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
        result.Value.ExpiresIn.Should().Be(1);
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.IdToken.Should().Be("anidtoken");
        _oidcService.Verify(s => s.RefreshTokenAsync(_caller.Object, "aclientid", "aclientsecret", "arefreshtoken",
            "ascope", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncWithUnsupportedGrantType_ThenReturnsError()
    {
        var result = await _application.ExchangeCodeForTokensAsync(_caller.Object, OAuth2GrantType.Password,
            "aclientid", "aclientsecret", "acode", "aredirecturi", "acodeverifier", "arefreshtoken",
            "ascope", CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.OpenIdConnectApplication_UnsupportedGrantType.Format(OAuth2GrantType.Password));
        _oidcService.Verify(
            s => s.ExchangeCodeForTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _oidcService.Verify(
            s => s.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}