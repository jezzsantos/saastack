using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.OAuth2;

namespace TestingStubApiHost.Api;

[BaseApiFrom("/microsoftidentity")]
public class StubMicrosoftIdentityApi : StubApiBase
{
    private readonly Dictionary<string, TokenContext> _tokenContexts = new();

    public StubMicrosoftIdentityApi(IRecorder recorder, IConfigurationSettings settings) : base(recorder, settings)
    {
    }

    public async Task<ApiPostResult<string, GenericOAuth2GrantAuthorizationResponse>> Authorize(
        GenericOAuth2GrantAuthorizationRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Recorder.TraceInformation(null,
            "StubMicrosoftIdentityApi: Authorize grant {GrantType}, with {Code} or RefreshToken {RefreshToken}, for scope {Scope}, with credentials {ClientId} and {ClientSecret}, and redirect to {RedirectUri}",
            request.GrantType ?? "none", request.Code ?? "none", request.RefreshToken ?? "none",
            request.Scope ?? "none", request.ClientId ?? "none", request.ClientSecret ?? "none",
            request.RedirectUri ?? "none");

        if (request.GrantType == OAuth2Constants.GrantTypes.AuthorizationCode)
        {
            if (request.Code.HasNoValue())
            {
                return () => Error.NotAuthenticated();
            }

            var (accessToken, refreshToken) = GenerateTokens();
            _tokenContexts.Add(refreshToken, new TokenContext(accessToken, request.Code));

            return () =>
                new PostResult<GenericOAuth2GrantAuthorizationResponse>(new GenericOAuth2GrantAuthorizationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = (int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds,
                    Scope = request.Scope,
                    IdToken = null,
                    TokenType = OAuth2Constants.TokenTypes.Bearer
                });
        }

        if (request.GrantType == OAuth2Constants.GrantTypes.RefreshToken)
        {
            if (request.RefreshToken.HasNoValue())
            {
                return () => Error.NotAuthenticated();
            }

            if (!_tokenContexts.TryGetValue(request.RefreshToken, out var tokenContext))
            {
                return () => Error.NotAuthenticated();
            }

            var (accessToken, refreshToken) = GenerateTokens();
            _tokenContexts.Add(refreshToken, new TokenContext(accessToken, tokenContext.Code));

            return () =>
                new PostResult<GenericOAuth2GrantAuthorizationResponse>(new GenericOAuth2GrantAuthorizationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = (int)AuthenticationConstants.Tokens.DefaultAccessTokenExpiry.TotalSeconds,
                    Scope = request.Scope,
                    IdToken = null,
                    TokenType = OAuth2Constants.TokenTypes.Bearer
                });
        }

        return () => Error.NotAuthenticated();
    }

    private static (string accessToken, string refreshToken) GenerateTokens()
    {
        var expiresOn = DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.GivenName, "agivenname"),
                new Claim(ClaimTypes.Surname, "asurname"),
                new Claim(AuthenticationConstants.Claims.ForTimezone, Timezones.Default.ToString()),
                new Claim(ClaimTypes.Country, CountryCodes.Default.ToString())
            ], expires: expiresOn,
            issuer: "MicrosoftIdentity"
        ));
        var refreshToken = Guid.NewGuid().ToString();

        return (accessToken, refreshToken);
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record TokenContext(string Token, string Code);
}