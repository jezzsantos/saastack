using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Web;
using ApiHost1;
using Application.Interfaces;
using Application.Resources.Shared;
using Common.Extensions;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using OAuth2CodeChallengeMethod = Application.Resources.Shared.OAuth2CodeChallengeMethod;
using OAuth2GrantType = Application.Resources.Shared.OAuth2GrantType;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;

namespace IdentityInfrastructure.IntegrationTests;

[UsedImplicitly]
public class OidcApiSpec
{
    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnUnauthenticatedUser : WebApiSpec<Program>
    {
        public GivenAnUnauthenticatedUser(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
        {
            EmptyAllRepositories();
        }

        [Fact]
        public async Task WhenGetAuthorize_ThenRedirectsToLogin()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");

            var result = await Api.GetAsync(new AuthorizeOAuth2GetRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
            });

            result.StatusCode.Should().Be(HttpStatusCode.Redirect);
            result.Headers.Location.Should().Be(WebsiteUiService.LoginPageRoute);
        }

        [Fact]
        public async Task WhenPostAuthorize_ThenRedirectsToLogin()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
            });

            result.StatusCode.Should().Be(HttpStatusCode.Redirect);
            result.Headers.Location.Should().Be(WebsiteUiService.LoginPageRoute);
        }

        [Fact]
        public async Task WhenCreateTokenAndUnknownClient_ThenReturnsError()
        {
            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = "oauthclient_1234567890123456789012",
                ClientSecret = "aclientsecret",
                Code = "anauthorizationcode",
                RedirectUri = "https://localhost/callback"
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidClient);
            result.Content.Error.Detail.Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownClient);
        }

        [Fact]
        public async Task WhenCreateTokenAndUnknownClientSecret_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = "aclientsecret",
                Code = "anauthorizationcode",
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidClient);
            result.Content.Error.Detail.Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownClient);
        }

        [Fact]
        public async Task WhenCreateTokenAndUnknownCode_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var clientSecret = await GenerateClientSecretAsync(login, client);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = "anauthorizationcode",
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidGrant);
            result.Content.Error.Detail.Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownAuthorizationCode);
        }

        [Fact]
        public async Task WhenCreateTokenAndDifferentClientRedirectUri_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = null,
                RedirectUri = "https://localhost/another"
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        [Fact]
        public async Task WhenCreateTokenWithPkceButMissingCodeVerifier_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes, OAuth2CodeChallengeMethod.Plain);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = null,
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        [Fact]
        public async Task WhenCreateTokenWithPkceButDifferentCodeVerifier_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes, OAuth2CodeChallengeMethod.Plain);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = "9994567890123456789012345678901234567890999",
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidGrant);
        }

        [Fact]
        public async Task WhenCreateTokenWithoutPkceButWithCodeVerifier_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = "1234567890123456789012345678901234567890123",
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Title.Should().Be(OAuth2Constants.ErrorCodes.InvalidRequest);
        }

        [Fact]
        public async Task WhenCreateTokenWithoutPkce_ThenReturnsToken()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = null,
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
            result.Content.Value.AccessToken.Should().NotBeNullOrEmpty();
            result.Content.Value.ExpiresIn.Should()
                .BeLessThanOrEqualTo((int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds);
            result.Content.Value.RefreshToken.Should().NotBeNullOrEmpty();
            result.Content.Value.IdToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenCreateTokenWithPkcePlain_ThenReturnsToken()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes, OAuth2CodeChallengeMethod.Plain);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = "1234567890123456789012345678901234567890123",
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
            result.Content.Value.AccessToken.Should().NotBeNullOrEmpty();
            result.Content.Value.ExpiresIn.Should()
                .BeLessThanOrEqualTo((int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds);
            result.Content.Value.RefreshToken.Should().NotBeNullOrEmpty();
            result.Content.Value.IdToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenCreateTokenWithPkceHash_ThenReturnsToken()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes, OAuth2CodeChallengeMethod.S256);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = "1234567890123456789012345678901234567890123",
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
            result.Content.Value.AccessToken.Should().NotBeNullOrEmpty();
            result.Content.Value.ExpiresIn.Should()
                .BeLessThanOrEqualTo((int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds);
            result.Content.Value.RefreshToken.Should().NotBeNullOrEmpty();
            result.Content.Value.IdToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenCreateToken_ThenReturnsTokens()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            var scopes =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";
            var clientSecret = await GenerateClientSecretAsync(login, client);
            await ConsentClientAsync(login, client, scopes);
            var authorizationCode = await AuthorizeAsync(login, client, scopes, OAuth2CodeChallengeMethod.S256);

            var result = await Api.PostAsync(new CreateOAuth2TokenRequest
            {
                GrantType = OAuth2GrantType.Authorization_Code,
                ClientId = client.Id,
                ClientSecret = clientSecret,
                Code = authorizationCode,
                CodeVerifier = "1234567890123456789012345678901234567890123",
                RedirectUri = client.RedirectUri
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
            result.Content.Value.AccessToken.Should().NotBeNullOrEmpty();
            result.Content.Value.ExpiresIn.Should()
                .BeLessThanOrEqualTo((int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds);
            result.Content.Value.RefreshToken.Should().NotBeNullOrEmpty();
            result.Content.Value.IdToken.Should().NotBeNullOrEmpty();

            var idToken = result.Content.Value.IdToken;
            var idTokenClaims = ReadTokenClaims(idToken);
            var nonce = idTokenClaims.Single(c => c.Type == AuthenticationConstants.Claims.ForNonce).Value;
            nonce.Should().Be("anonce");
            var email = idTokenClaims.Single(c => c.Type == AuthenticationConstants.Claims.ForEmail).Value;
            email.Should().Be(login.Profile!.EmailAddress);
            var name = idTokenClaims.Single(c => c.Type == AuthenticationConstants.Claims.ForFullName).Value;
            name.Should().Be($"{login.Profile!.Name.FirstName} {login.Profile!.Name.LastName}");
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
            document.ResponseTypesSupported.Should().Contain(OAuth2Constants.ResponseTypes.Code);

            // OIDC Core 1.0 Section 3 - Verify required scopes
            document.ScopesSupported.Should().Contain(OpenIdConnectConstants.Scopes.OpenId);
            document.ScopesSupported.Should().Contain(OAuth2Constants.Scopes.Profile);
            document.ScopesSupported.Should().Contain(OAuth2Constants.Scopes.Email);

            // OIDC Core 1.0 Section 3 - Verify subject types
            document.SubjectTypesSupported.Should().Contain(OAuth2Constants.SubjectTypes.Public);

            // OIDC Core 1.0 Section 3 - Verify signing algorithms
            document.IdTokenSigningAlgValuesSupported.Should().Contain(OAuth2Constants.SigningAlgorithms.Rs256);

            // OIDC Core 1.0 Section 3 - Verify token endpoint auth methods
            document.TokenEndpointAuthMethodsSupported.Should().NotBeEmpty();
            document.TokenEndpointAuthMethodsSupported.Should()
                .Contain(OAuth2Constants.ClientAuthenticationMethods.ClientSecretPost);

            // OIDC Core 1.0 Section 3 - Verify claims supported
            document.ClaimsSupported.Should().NotBeEmpty();
            document.ClaimsSupported.Should().Contain(OAuth2Constants.StandardClaims.Subject);
            document.ClaimsSupported.Should().Contain(OAuth2Constants.StandardClaims.Name);
            document.ClaimsSupported.Should().Contain(OAuth2Constants.StandardClaims.Email);

            // PKCE support verification
            document.CodeChallengeMethodsSupported.Should().NotBeEmpty();
            document.CodeChallengeMethodsSupported.Should().Contain(OAuth2Constants.CodeChallengeMethods.S256);
        }

        [Fact]
        public async Task WhenGetJsonWebKeySet_ThenReturnsValidJwks()
        {
            var result = await Api.GetAsync(new GetJsonWebKeySetRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Keys.Should().NotBeNull();
            result.Content.Value.Keys.Keys.Should().NotBeEmpty();

            var firstKey = Enumerable.First(result.Content.Value.Keys.Keys);

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

        private static List<Claim> ReadTokenClaims(string? idToken)
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(idToken).Claims.ToList();
        }

        private async Task<string> AuthorizeAsync(LoginDetails login, OAuth2Client client, string scopes,
            OAuth2CodeChallengeMethod? codeChallengeMethod = null)
        {
            var codeChallenge = codeChallengeMethod switch
            {
                OAuth2CodeChallengeMethod.Plain => "1234567890123456789012345678901234567890123",
                OAuth2CodeChallengeMethod.S256 => "WWHTYIjNclXxS69q1gerQ+eTlW5ab1YCpKTorurQ3zw=",
                _ => null
            };
            var response = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = scopes,
                State = "astate",
                Nonce = "anonce",
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod
            }, req => req.SetJWTBearerToken(login.AccessToken));

            return ParseLocation(response, "code");
        }

        private static string ParseLocation<TResponse>(JsonResponse<TResponse> response, string parameterName)
            where TResponse : IWebResponse
        {
            return HttpUtility.ParseQueryString(response.Headers.Location!.Query)[parameterName]!;
        }

        private async Task<OAuth2Client> CreateClientAsync(LoginDetails login, string name = "aclientname",
            string? redirectUri = null)
        {
            return (await Api.PostAsync(new CreateOAuth2ClientRequest
            {
                Name = name,
                RedirectUri = redirectUri
            }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;
        }

        private async Task ConsentClientAsync(LoginDetails login, OAuth2Client client,
            string scopes)
        {
            await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
            {
                Id = client.Id,
                Scope = scopes,
                Consented = true
            }, req => req.SetJWTBearerToken(login.AccessToken));
        }

        private async Task<string> GenerateClientSecretAsync(LoginDetails login, OAuth2Client client)
        {
            var secret = (await Api.PostAsync(new RegenerateOAuth2ClientSecretRequest
            {
                Id = client.Id
            }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client!.Secret;

            return secret;
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
        public async Task WhenPostAuthorizeAndWrongCode_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Token,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Found);
            ParseLocationRedirectUri(result).Should().Be(client.RedirectUri);
            ParseLocationParameter(result, "error").Should().Be(OAuth2Constants.ErrorCodes.UnsupportedResponseType);
            ParseLocationParameter(result, "error_description").Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_Authorize_UnsupportedResponseType
                .Format(OAuth2ResponseType.Token));
        }

        [Fact]
        public async Task WhenPostAuthorizeAndMissingOpenIdScope_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OAuth2Constants.Scopes.Profile}"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Found);
            ParseLocationRedirectUri(result).Should().Be(client.RedirectUri);
            ParseLocationParameter(result, "error").Should().Be(OAuth2Constants.ErrorCodes.InvalidScope);
            ParseLocationParameter(result, "error_description").Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_Authorize_MissingOpenIdScope);
        }

        [Fact]
        public async Task WhenPostAuthorizeAndMissingCodeChallengeMethod_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}",
                CodeChallenge = "1234567890123456789012345678901234567890123",
                CodeChallengeMethod = null
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Found);
            ParseLocationRedirectUri(result).Should().Be(client.RedirectUri);
            ParseLocationParameter(result, "error").Should().Be(OAuth2Constants.ErrorCodes.InvalidRequest);
            ParseLocationParameter(result, "error_description").Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_Authorize_MissingCodeChallengeMethod);
        }

        [Fact]
        public async Task WhenPostAuthorizeAndUnknownClientId_ThenReturnsError()
        {
            var login = await LoginUserAsync();

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = "oauthclient_1234567890123456789012",
                RedirectUri = "https://localhost/callback",
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Found);
            ParseLocationRedirectUri(result).Should().Be("https://localhost/callback");
            ParseLocationParameter(result, "error").Should().Be(OAuth2Constants.ErrorCodes.InvalidClient);
            ParseLocationParameter(result, "error_description").Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_Authorize_UnknownClient);
        }

        [Fact]
        public async Task WhenPostAuthorizeAndClientWithoutRedirectUri_ThenReturnsError()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login);

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = "https://localhost/callback",
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Found);
            ParseLocationRedirectUri(result).Should().Be("https://localhost/callback");
            ParseLocationParameter(result, "error").Should().Be(OAuth2Constants.ErrorCodes.InvalidRequest);
            ParseLocationParameter(result, "error_description").Should().Be(IdentityApplication.Resources
                .NativeIdentityServerOpenIdConnectService_Authorize_MismatchedRequestUri
                .Format("https://localhost/callback"));
        }

        [Fact]
        public async Task WhenPostAuthorizeAndClientIsUnconsented_ThenRedirectsToConsentPage()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Redirect);
            result.Headers.Location.Should().Be(WebsiteUiService.OAuth2ConsentPageRoute);
        }

        [Fact]
        public async Task WhenPostAuthorizeAndClientIsConsentedToDifferentScopes_ThenRedirectsToConsentPage()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            await ConsentClientAsync(login, client,
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}  {OAuth2Constants.Scopes.Email}"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Redirect);
            result.Headers.Location.Should().Be(WebsiteUiService.OAuth2ConsentPageRoute);
        }

        [Fact]
        public async Task WhenPostAuthorizeAndClientIsConsented_ThenReturnsCode()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            await ConsentClientAsync(login, client,
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}  {OAuth2Constants.Scopes.Profile}"
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Found);
            ParseLocationRedirectUri(result).Should().Be(client.RedirectUri);
            ParseLocationParameter(result, "code").Should().NotBeNullOrEmpty();
            ParseLocationParameter(result, "state").Should().BeNull();
        }

        [Fact]
        public async Task WhenPostAuthorizeAndClientIsConsentedWithAllSecurity_ThenReturnsCode()
        {
            var login = await LoginUserAsync();
            var client = await CreateClientAsync(login, redirectUri: "https://localhost/callback");
            await ConsentClientAsync(login, client,
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}");

            var result = await Api.PostAsync(new AuthorizeOAuth2PostRequest
            {
                ClientId = client.Id,
                RedirectUri = client.RedirectUri,
                ResponseType = OAuth2ResponseType.Code,
                Scope = $"{OpenIdConnectConstants.Scopes.OpenId}  {OAuth2Constants.Scopes.Profile}",
                State = "astate",
                Nonce = "anonce",
                CodeChallenge = "1234567890123456789012345678901234567890123",
                CodeChallengeMethod = OAuth2CodeChallengeMethod.S256
            }, req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.Found);
            ParseLocationRedirectUri(result).Should().Be(client.RedirectUri);
            ParseLocationParameter(result, "code").Should().NotBeNullOrEmpty();
            ParseLocationParameter(result, "state").Should().Be("astate");
        }

        [Fact]
        public async Task WhenGetUserInfo_ThenReturnsOidcCompliantUserInfo()
        {
            var login = await LoginUserAsync();

            var result = await Api.GetAsync(new GetUserInfoForCallerRequest(),
                req => req.SetJWTBearerToken(login.AccessToken));

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.User.Should().NotBeNull();

            var userInfo = result.Content.Value.User;

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

        private static string ParseLocationRedirectUri<TResponse>(JsonResponse<TResponse> response)
            where TResponse : IWebResponse
        {
            return response.Headers.Location!.GetLeftPart(UriPartial.Path);
        }

        private static string ParseLocationParameter<TResponse>(JsonResponse<TResponse> response, string parameterName)
            where TResponse : IWebResponse
        {
            return HttpUtility.ParseQueryString(response.Headers.Location!.Query)[parameterName]!;
        }

        private async Task ConsentClientAsync(LoginDetails login, OAuth2Client client,
            string scopes)
        {
            await Api.PostAsync(new ConsentOAuth2ClientForCallerRequest
            {
                Id = client.Id,
                Scope = scopes,
                Consented = true
            }, req => req.SetJWTBearerToken(login.AccessToken));
        }

        private async Task<OAuth2Client> CreateClientAsync(LoginDetails login, string name = "aclientname",
            string? redirectUri = null)
        {
            return (await Api.PostAsync(new CreateOAuth2ClientRequest
            {
                Name = name,
                RedirectUri = redirectUri
            }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;
        }

        private static void OverrideDependencies(IServiceCollection services)
        {
        }
    }
}