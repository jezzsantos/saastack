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
using OAuth2CodeChallengeMethod = Application.Resources.Shared.OAuth2CodeChallengeMethod;
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
        var expectedDocument = new OpenIdConnectDiscoveryDocument
        {
            Issuer = "anissuer",
            AuthorizationEndpoint = $"anissuer{OAuth2Constants.Endpoints.Authorization}",
            TokenEndpoint = $"anissuer{OAuth2Constants.Endpoints.Token}",
            UserInfoEndpoint = $"anissuer{OAuth2Constants.Endpoints.UserInfo}",
            JwksUri = $"anissuer{OpenIdConnectConstants.Endpoints.Jwks}",
            ResponseTypesSupported = [OAuth2Constants.ResponseTypes.Code],
            ScopesSupported =
            [
                OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile,
                OAuth2Constants.Scopes.Email
            ],
            SubjectTypesSupported = [OAuth2Constants.SubjectTypes.Public],
            IdTokenSigningAlgValuesSupported =
                [OAuth2Constants.SigningAlgorithms.Hs256, OAuth2Constants.SigningAlgorithms.Rs256],
            TokenEndpointAuthMethodsSupported =
            [
                OAuth2Constants.ClientAuthenticationMethods.ClientSecretBasic,
                OAuth2Constants.ClientAuthenticationMethods.ClientSecretPost
            ],
            ClaimsSupported =
            [
                OAuth2Constants.StandardClaims.Subject, OAuth2Constants.StandardClaims.Name,
                OAuth2Constants.StandardClaims.Email, OAuth2Constants.StandardClaims.EmailVerified
            ],
            CodeChallengeMethodsSupported = [OAuth2Constants.CodeChallengeMethods.S256]
        };

        _oidcService.Setup(s => s.GetDiscoveryDocumentAsync(_caller.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocument);

        var result = await _application.GetDiscoveryDocumentAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Issuer.Should().Be("anissuer");
        result.Value.AuthorizationEndpoint.Should().Be($"anissuer{OAuth2Constants.Endpoints.Authorization}");
        result.Value.TokenEndpoint.Should().Be($"anissuer{OAuth2Constants.Endpoints.Token}");
        result.Value.UserInfoEndpoint.Should().Be($"anissuer{OAuth2Constants.Endpoints.UserInfo}");
        result.Value.JwksUri.Should().Be($"anissuer{OpenIdConnectConstants.Endpoints.Jwks}");
        result.Value.ResponseTypesSupported.Should().Contain(OAuth2Constants.ResponseTypes.Code);
        result.Value.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.OpenId);
        result.Value.ScopesSupported.Should().Contain(OAuth2Constants.Scopes.Profile);
        result.Value.ScopesSupported.Should().Contain(OAuth2Constants.Scopes.Email);
    }

    [Fact]
    public async Task WhenGetJsonWebKeySetAsync_ThenReturnsJwks()
    {
        var expectedJwks = new JsonWebKeySet
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
            .ReturnsAsync(expectedJwks);

        var result = await _application.GetJsonWebKeySetAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Keys.Should().HaveCount(1);
        result.Value.Keys[0].Kty.Should().Be("akeytype");
        result.Value.Keys[0].Use.Should().Be("akeyuse");
        result.Value.Keys[0].Kid.Should().Be("akeyid");
        result.Value.Keys[0].Alg.Should().Be(OAuth2Constants.SigningAlgorithms.Hs512);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncWith_ThenReturnsAuthorizationCode()
    {
        _oidcService.Setup(s => s.AuthorizeAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OAuth2ResponseType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<OAuth2CodeChallengeMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenIdConnectAuthorization
            {
                Code = new OpenIdConnectAuthorizationCode
                {
                    Code = "anauthorizationcode",
                    State = "astate"
                }
            });

        var result = await _application.AuthorizeAsync(_caller.Object, "aclientid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", "acodechallenge", OAuth2CodeChallengeMethod.Plain,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Code!.Code.Should().Be("anauthorizationcode");
        result.Value.Code.State.Should().Be("astate");
        _oidcService.Verify(s => s.AuthorizeAsync(_caller.Object, "aclientid", "acallerid", "aredirecturi",
            OAuth2ResponseType.Code, "ascope", "astate", "anonce", "acodechallenge", OAuth2CodeChallengeMethod.Plain,
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenCreateTokenAsyncWithAuthorizationCode_ThenReturnsTokens()
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

        var result = await _application.CreateTokenAsync(_caller.Object, OAuth2GrantType.Authorization_Code,
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
    public async Task WhenCreateTokenAsyncWithRefreshToken_ThenReturnsNewTokens()
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

        var result = await _application.CreateTokenAsync(_caller.Object, OAuth2GrantType.Refresh_Token,
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
    public async Task WhenCreateTokenAsyncWithUnsupportedGrantType_ThenReturnsError()
    {
        var result = await _application.CreateTokenAsync(_caller.Object, OAuth2GrantType.Password,
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