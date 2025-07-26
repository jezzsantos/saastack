using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using FluentAssertions;
using IdentityDomain;
using Moq;
using UnitTesting.Common;
using Xunit;

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
        _caller.Setup(c => c.CallerId).Returns("acallerid");

        _oidcService = new Mock<IIdentityServerOpenIdConnectService>();
        var identityServerProvider = new Mock<IIdentityServerProvider>();
        identityServerProvider.Setup(p => p.OpenIdConnectService)
            .Returns(_oidcService.Object);

        _application = new OpenIdConnectApplication(identityServerProvider.Object);
    }

    [Fact]
    public async Task WhenGetDiscoveryDocumentAsync_ThenReturnsDiscoveryDocument()
    {
        var expectedDocument = new OidcDiscoveryDocument
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
    public async Task WhenAuthorizeAsyncWithValidRequest_ThenReturnsAuthorizationCode()
    {
        var expectedResponse = new OidcAuthorizationResponse
        {
            Code = "anauthorizationcode",
            State = "astate"
        };

        _oidcService.Setup(s => s.AuthorizeAsync(
                _caller.Object,
                "aclientid",
                "aredirecturi",
                OAuth2Constants.ResponseTypes.Code,
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}",
                "astate",
                "anonce",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _application.AuthorizeAsync(
            _caller.Object,
            "aclientid",
            "aredirecturi",
            OAuth2Constants.ResponseTypes.Code,
            $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}",
            "astate",
            "anonce",
            null,
            null,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Code.Should().NotBeEmpty();
        result.Value.State.Should().Be("astate");
    }

    [Fact]
    public async Task WhenAuthorizeAsyncWithInvalidResponseType_ThenReturnsError()
    {
        _oidcService.Setup(s => s.AuthorizeAsync(
                _caller.Object,
                "aclientid",
                "aredirecturi",
                OAuth2Constants.ResponseTypes.Token,
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Validation("avalidationmessage"));

        var result = await _application.AuthorizeAsync(
            _caller.Object,
            "aclientid",
            "aredirecturi",
            OAuth2Constants.ResponseTypes.Token, // Invalid response type
            $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}",
            null,
            null,
            null,
            null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation);
        result.Error.Message.Should().Contain("avalidationmessage");
    }

    [Fact]
    public async Task WhenAuthorizeAsyncWithoutOpenIdScope_ThenReturnsError()
    {
        _oidcService.Setup(s => s.AuthorizeAsync(
                _caller.Object,
                "aclientid",
                "aredirecturi",
                OAuth2Constants.ResponseTypes.Code,
                $"{OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Validation("avalidationmessage"));

        var result = await _application.AuthorizeAsync(
            _caller.Object,
            "aclientid",
            "aredirecturi",
            OAuth2Constants.ResponseTypes.Code,
            $"{OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}", // Missing openid scope
            null,
            null,
            null,
            null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation);
        result.Error.Message.Should().Contain("avalidationmessage");
    }

    [Fact]
    public async Task WhenCreateTokenAsyncWithAuthorizationCode_ThenReturnsTokens()
    {
        var expectedTokenResponse = new OidcTokenResponse
        {
            AccessToken = "anaccesstoken",
            TokenType = OAuth2Constants.TokenTypes.Bearer,
            ExpiresIn = 900, // 15 minutes
            RefreshToken = "arefreshtoken",
            IdToken = "anidtoken",
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}"
        };

        _oidcService.Setup(s => s.ExchangeCodeForTokensAsync(
                _caller.Object,
                "aclientid",
                "aclientsecret",
                "anauthorizationcode",
                null,
                "aredirecturi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokenResponse);

        var result = await _application.CreateTokenAsync(
            _caller.Object,
            OAuth2Constants.GrantTypes.AuthorizationCode,
            "aclientid",
            "aclientsecret",
            "anauthorizationcode",
            null,
            "aredirecturi",
            "",
            null,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.TokenType.Should().Be(OAuth2Constants.TokenTypes.Bearer);
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.IdToken.Should().Be("anidtoken");
        result.Value.Scope.Should()
            .Be(
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}");
    }

    [Fact]
    public async Task WhenCreateTokenAsyncWithRefreshToken_ThenReturnsNewTokens()
    {
        var expectedTokenResponse = new OidcTokenResponse
        {
            AccessToken = "anewaccesstoken",
            TokenType = OAuth2Constants.TokenTypes.Bearer,
            ExpiresIn = 900, // 15 minutes
            RefreshToken = "anewrefreshtoken",
            IdToken = "anewidtoken",
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}"
        };

        _oidcService.Setup(s => s.RefreshTokenAsync(
                _caller.Object,
                "aclientid",
                "aclientsecret",
                "anoldrefreshtoken",
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokenResponse);

        var result = await _application.CreateTokenAsync(
            _caller.Object,
            OAuth2Constants.GrantTypes.RefreshToken,
            "aclientid",
            "aclientsecret",
            "",
            null,
            "",
            "anoldrefreshtoken",
            $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("anewaccesstoken");
        result.Value.TokenType.Should().Be(OAuth2Constants.TokenTypes.Bearer);
        result.Value.RefreshToken.Should().Be("anewrefreshtoken");
        result.Value.Scope.Should()
            .Be($"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}");
    }

    [Fact]
    public async Task WhenCreateTokenAsyncWithUnsupportedGrantType_ThenReturnsError()
    {
        var result = await _application.CreateTokenAsync(
            _caller.Object,
            OAuth2Constants.GrantTypes.ClientCredentials, // Unsupported grant type
            "aclientid",
            "aclientsecret",
            "",
            null,
            "",
            "",
            null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation);
        result.Error.Message.Should()
            .Contain($"Unsupported grant type: '{OAuth2Constants.GrantTypes.ClientCredentials}'");
    }
}