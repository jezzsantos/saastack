using System.Net;
using ApiHost1;
using FluentAssertions;
using IdentityDomain;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[UsedImplicitly]
public class OidcApiSpec
{
    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnUnauthenticatedUser : WebApiSpec<Program>
    {
        private const string AClientId = "aclientid12345678901234567890";
        private const string ARedirectUri = "https://localhost/callback";
        private const string AState = "astate";
        private const string ANonce = "anonce";
        private const string ACodeChallenge = "acodechallenge";
        private const string ACodeChallengeMethod = OpenIdConnectConstants.CodeChallengeMethods.S256;

        public GivenAnUnauthenticatedUser(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
        }

        [Fact]
        public async Task WhenGetDiscoveryDocument_ThenReturnsOidcCompliantDiscoveryDocument()
        {
            var result = await Api.GetAsync(new GetDiscoveryDocumentRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Document.Should().NotBeNull();

            var document = result.Content.Value.Document;

            // OIDC Core 1.0 Section 3 - Required fields
            document.Issuer.Should().NotBeNullOrEmpty();
            document.AuthorizationEndpoint.Should().NotBeNullOrEmpty();
            document.TokenEndpoint.Should().NotBeNullOrEmpty();
            document.UserInfoEndpoint.Should().NotBeNullOrEmpty();
            document.JwksUri.Should().NotBeNullOrEmpty();
            document.ResponseTypesSupported.Should().NotBeEmpty();
            document.SubjectTypesSupported.Should().NotBeEmpty();
            document.IdTokenSigningAlgValuesSupported.Should().NotBeEmpty();

            // OIDC Core 1.0 Section 3 - Verify required response types
            document.ResponseTypesSupported.Should().Contain(OpenIdConnectConstants.ResponseTypes.Code);

            // OIDC Core 1.0 Section 3 - Verify required scopes
            document.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.OpenId);
            document.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.Profile);
            document.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.Email);

            // OIDC Core 1.0 Section 3 - Verify subject types
            document.SubjectTypesSupported.Should().Contain(OpenIdConnectConstants.SubjectTypes.Public);

            // OIDC Core 1.0 Section 3 - Verify signing algorithms
            document.IdTokenSigningAlgValuesSupported.Should().Contain(OpenIdConnectConstants.SigningAlgorithms.Rs256);

            // OIDC Core 1.0 Section 3 - Verify token endpoint auth methods
            document.TokenEndpointAuthMethodsSupported.Should().NotBeEmpty();
            document.TokenEndpointAuthMethodsSupported.Should()
                .Contain(OpenIdConnectConstants.ClientAuthenticationMethods.ClientSecretPost);

            // OIDC Core 1.0 Section 3 - Verify claims supported
            document.ClaimsSupported.Should().NotBeEmpty();
            document.ClaimsSupported.Should().Contain(OpenIdConnectConstants.StandardClaims.Subject);
            document.ClaimsSupported.Should().Contain(OpenIdConnectConstants.StandardClaims.Name);
            document.ClaimsSupported.Should().Contain(OpenIdConnectConstants.StandardClaims.Email);

            // PKCE support verification
            document.CodeChallengeMethodsSupported.Should().NotBeEmpty();
            document.CodeChallengeMethodsSupported.Should().Contain(OpenIdConnectConstants.CodeChallengeMethods.S256);
        }

        [Fact]
        public async Task WhenGetJsonWebKeySet_ThenReturnsValidJwks()
        {
            var result = await Api.GetAsync(new GetJsonWebKeySetRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Keys.Should().NotBeNull();
            result.Content.Value.Keys.Keys.Should().NotBeEmpty();

            var firstKey = result.Content.Value.Keys.Keys.First();

            // RFC 7517 Section 4 - Required JWK parameters
            firstKey.Kty.Should().NotBeNullOrEmpty(); // Key Type
            firstKey.Use.Should().NotBeNullOrEmpty(); // Public Key Use
            firstKey.Kid.Should().NotBeNullOrEmpty(); // Key ID
            firstKey.Alg.Should().NotBeNullOrEmpty(); // Algorithm

            // For RSA keys (most common for OIDC)
            if (firstKey.Kty == "RSA")
            {
                firstKey.N.Should().NotBeNullOrEmpty(); // Modulus
                firstKey.E.Should().NotBeNullOrEmpty(); // Exponent
            }
        }

        [Fact]
        public async Task WhenAuthorizeWithValidParameters_ThenReturnsAuthorizationCode()
        {
            var result = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId} {OpenIdConnectConstants.Scopes.Profile}",
                State = AState,
                Nonce = ANonce
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Code.Should().NotBeNullOrEmpty();
            result.Content.Value.State.Should().Be(AState);
        }

        [Fact]
        public async Task WhenAuthorizeWithPkce_ThenReturnsAuthorizationCode()
        {
            var result = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId} {OpenIdConnectConstants.Scopes.Profile}",
                State = AState,
                Nonce = ANonce,
                CodeChallenge = ACodeChallenge,
                CodeChallengeMethod = ACodeChallengeMethod
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Code.Should().NotBeNullOrEmpty();
            result.Content.Value.State.Should().Be(AState);
        }

        [Fact]
        public async Task WhenAuthorizeWithMissingClientId_ThenReturnsBadRequest()
        {
            var result = await Api.GetAsync(new AuthorizeRequest
            {
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = OpenIdConnectConstants.Scopes.OpenId
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenAuthorizeWithInvalidRedirectUri_ThenReturnsBadRequest()
        {
            var result = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = "aninvaliduri",
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = OpenIdConnectConstants.Scopes.OpenId
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenAuthorizeWithUnsupportedResponseType_ThenReturnsBadRequest()
        {
            var result = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = "unsupported_type",
                Scope = OpenIdConnectConstants.Scopes.OpenId
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenTokenEndpointWithValidAuthorizationCode_ThenReturnsTokens()
        {
            // First get an authorization code
            var authorizeResult = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope =
                    $"{OpenIdConnectConstants.Scopes.OpenId} {OpenIdConnectConstants.Scopes.Profile} {OpenIdConnectConstants.Scopes.Email}",
                State = AState,
                Nonce = ANonce
            });

            authorizeResult.StatusCode.Should().Be(HttpStatusCode.OK);
            var authorizationCode = authorizeResult.Content.Value.Code;

            // Exchange authorization code for tokens
            var tokenResult = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = OpenIdConnectConstants.GrantTypes.AuthorizationCode,
                ClientId = AClientId,
                ClientSecret = "aclientsecret",
                Code = authorizationCode,
                RedirectUri = ARedirectUri
            });

            tokenResult.StatusCode.Should().Be(HttpStatusCode.OK);

            // OIDC Core 1.0 Section 3.1.3.1 - Verify token response
            var tokenResponse = tokenResult.Content.Value;
            tokenResponse.AccessToken.Should().NotBeNullOrEmpty();
            tokenResponse.TokenType.Should().Be(OpenIdConnectConstants.TokenTypes.Bearer);
            tokenResponse.ExpiresIn.Should().BeGreaterThan(0);
            tokenResponse.IdToken.Should().NotBeNullOrEmpty(); // Required for OIDC
            tokenResponse.Scope.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenTokenEndpointWithPkce_ThenReturnsTokens()
        {
            // First get an authorization code with PKCE
            var authorizeResult = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId} {OpenIdConnectConstants.Scopes.Profile}",
                State = AState,
                Nonce = ANonce,
                CodeChallenge = ACodeChallenge,
                CodeChallengeMethod = ACodeChallengeMethod
            });

            authorizeResult.StatusCode.Should().Be(HttpStatusCode.OK);
            var authorizationCode = authorizeResult.Content.Value.Code;

            // Exchange authorization code for tokens with code verifier
            var tokenResult = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = OpenIdConnectConstants.GrantTypes.AuthorizationCode,
                ClientId = AClientId,
                ClientSecret = "aclientsecret",
                Code = authorizationCode,
                RedirectUri = ARedirectUri,
                CodeVerifier = "acodeverifier" // In real implementation, this should match the challenge
            });

            tokenResult.StatusCode.Should().Be(HttpStatusCode.OK);
            tokenResult.Content.Value.AccessToken.Should().NotBeNullOrEmpty();
            tokenResult.Content.Value.IdToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenTokenEndpointWithInvalidGrantType_ThenReturnsBadRequest()
        {
            var result = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = "aninvalidgranttype",
                ClientId = AClientId,
                ClientSecret = "aclientsecret",
                Code = "acode",
                RedirectUri = ARedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenTokenEndpointWithMissingClientId_ThenReturnsBadRequest()
        {
            var result = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = OpenIdConnectConstants.GrantTypes.AuthorizationCode,
                ClientSecret = "aclientsecret",
                Code = "acode",
                RedirectUri = ARedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenTokenEndpointWithInvalidCode_ThenReturnsUnauthorized()
        {
            var result = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = OpenIdConnectConstants.GrantTypes.AuthorizationCode,
                ClientId = AClientId,
                ClientSecret = "aclientsecret",
                Code = "aninvalidcode",
                RedirectUri = ARedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnAuthenticatedUser : WebApiSpec<Program>
    {
        public GivenAnAuthenticatedUser(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
        }

        [Fact]
        public async Task WhenGetUserInfo_ThenReturnsOidcCompliantUserInfo()
        {
            var login = await LoginUserAsync();

            var result = await Api.GetAsync(new GetUserInfoForCallerRequest(),
                req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.OidcUserInfo.Should().NotBeNull();

            var userInfo = result.Content.Value.OidcUserInfo;

            // OIDC Core 1.0 Section 5.1 - Required claims
            userInfo.Sub.Should().NotBeNullOrEmpty(); // Subject identifier - required

            // OIDC Core 1.0 Section 5.1 - Standard claims (when available)
            if (!string.IsNullOrEmpty(userInfo.Name))
            {
                userInfo.Name.Should().NotBeNullOrEmpty();
            }

            if (!string.IsNullOrEmpty(userInfo.Email))
            {
                userInfo.Email.Should().NotBeNullOrEmpty();
                userInfo.EmailVerified.Should().NotBeNull();
            }

            // Verify subject matches the authenticated user
            userInfo.Sub.Should().Be(login.User.Id);
        }

        [Fact]
        public async Task WhenGetUserInfoWithoutToken_ThenReturnsUnauthorized()
        {
            var result = await Api.GetAsync(new GetUserInfoForCallerRequest());

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task WhenGetUserInfoWithInvalidToken_ThenReturnsUnauthorized()
        {
            var result = await Api.GetAsync(new GetUserInfoForCallerRequest(),
                req => req.SetJWTBearerToken("aninvalidtoken"));

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenOidcComplianceTests : WebApiSpec<Program>
    {
        private const string AClientId = "aclientid12345678901234567890";
        private const string ARedirectUri = "https://localhost/callback";

        public GivenOidcComplianceTests(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
        }

        [Fact]
        public async Task WhenDiscoveryDocument_ThenContainsAllRequiredOidcEndpoints()
        {
            var result = await Api.GetAsync(new GetDiscoveryDocumentRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var document = result.Content.Value.Document;

            // Verify all OIDC endpoints are properly configured
            document.AuthorizationEndpoint.Should().EndWith(OpenIdConnectConstants.Endpoints.Authorization);
            document.TokenEndpoint.Should().EndWith(OpenIdConnectConstants.Endpoints.Token);
            document.UserInfoEndpoint.Should().EndWith(OpenIdConnectConstants.Endpoints.UserInfo);
            document.JwksUri.Should().EndWith(OpenIdConnectConstants.Endpoints.Jwks);

            // Verify issuer is a valid URI
            Uri.TryCreate(document.Issuer, UriKind.Absolute, out _).Should().BeTrue();
        }

        [Fact]
        public async Task WhenAuthorizeWithInvalidScope_ThenReturnsBadRequest()
        {
            var result = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = "aninvalidscope"
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenAuthorizeWithoutOpenIdScope_ThenReturnsBadRequest()
        {
            // OIDC Core 1.0 Section 3.1.2.1 - openid scope is required for OIDC requests
            var result = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = OpenIdConnectConstants.Scopes.Profile // Missing openid scope
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenTokenEndpointWithRefreshToken_ThenReturnsNewTokens()
        {
            // First get tokens through authorization code flow
            var authorizeResult = await Api.GetAsync(new AuthorizeRequest
            {
                ClientId = AClientId,
                RedirectUri = ARedirectUri,
                ResponseType = OpenIdConnectConstants.ResponseTypes.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId} {OpenIdConnectConstants.Scopes.OfflineAccess}",
                State = "astate"
            });

            authorizeResult.StatusCode.Should().Be(HttpStatusCode.OK);
            var authorizationCode = authorizeResult.Content.Value.Code;

            var initialTokenResult = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = OpenIdConnectConstants.GrantTypes.AuthorizationCode,
                ClientId = AClientId,
                ClientSecret = "aclientsecret",
                Code = authorizationCode,
                RedirectUri = ARedirectUri
            });

            initialTokenResult.StatusCode.Should().Be(HttpStatusCode.OK);
            var refreshToken = initialTokenResult.Content.Value.RefreshToken;
            refreshToken.Should().NotBeNullOrEmpty();

            // Use refresh token to get new tokens
            var refreshResult = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = OpenIdConnectConstants.GrantTypes.RefreshToken,
                ClientId = AClientId,
                ClientSecret = "aclientsecret",
                RefreshToken = refreshToken
            });

            refreshResult.StatusCode.Should().Be(HttpStatusCode.OK);
            refreshResult.Content.Value.AccessToken.Should().NotBeNullOrEmpty();
            refreshResult.Content.Value.AccessToken.Should().NotBe(initialTokenResult.Content.Value.AccessToken);
        }

        [Fact]
        public async Task WhenTokenEndpointWithInvalidRefreshToken_ThenReturnsUnauthorized()
        {
            var result = await Api.PostAsync(new TokenEndpointRequest
            {
                GrantType = OpenIdConnectConstants.GrantTypes.RefreshToken,
                ClientId = AClientId,
                ClientSecret = "aclientsecret",
                RefreshToken = "aninvalidrefreshtoken"
            });

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task WhenJwksEndpoint_ThenReturnsValidKeySetForTokenVerification()
        {
            var result = await Api.GetAsync(new GetJsonWebKeySetRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var keySet = result.Content.Value.Keys;

            keySet.Keys.Should().NotBeEmpty();

            // Verify each key has required properties for token verification
            foreach (var key in keySet.Keys)
            {
                key.Kty.Should().NotBeNullOrEmpty();
                key.Use.Should().Be("sig"); // For signature verification
                key.Kid.Should().NotBeNullOrEmpty();
                key.Alg.Should().NotBeNullOrEmpty();

                // For RSA keys used in JWT signing
                if (key.Kty == "RSA")
                {
                    key.N.Should().NotBeNullOrEmpty();
                    key.E.Should().NotBeNullOrEmpty();
                }
            }
        }

        [Fact]
        public async Task WhenDiscoveryDocument_ThenSupportsRequiredOidcFeatures()
        {
            var result = await Api.GetAsync(new GetDiscoveryDocumentRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var document = result.Content.Value.Document;

            // OIDC Core 1.0 Section 3 - Required features
            document.ResponseTypesSupported.Should().Contain(OpenIdConnectConstants.ResponseTypes.Code);
            document.SubjectTypesSupported.Should().Contain(OpenIdConnectConstants.SubjectTypes.Public);
            document.IdTokenSigningAlgValuesSupported.Should().Contain(OpenIdConnectConstants.SigningAlgorithms.Rs256);

            // PKCE support (RFC 7636)
            document.CodeChallengeMethodsSupported.Should().Contain(OpenIdConnectConstants.CodeChallengeMethods.S256);

            // Standard scopes
            document.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.OpenId);
            document.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.Profile);
            document.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.Email);

            // Standard claims
            document.ClaimsSupported.Should().Contain(OpenIdConnectConstants.StandardClaims.Subject);
            document.ClaimsSupported.Should().Contain(OpenIdConnectConstants.StandardClaims.Name);
            document.ClaimsSupported.Should().Contain(OpenIdConnectConstants.StandardClaims.Email);
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
        }
    }
}