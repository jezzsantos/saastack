using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using IdentityApplication.ApplicationServices;
using Infrastructure.Interfaces;
using Infrastructure.Shared.ApplicationServices.External;

namespace IdentityInfrastructure.ApplicationServices;

/// <summary>
///     Provides a <see cref="ISSOAuthenticationProvider" /> for handling Microsoft Identity SSO
/// </summary>
public class MicrosoftSSOAuthenticationProvider : ISSOAuthenticationProvider
{
    public const string SSOName = "microsoft";
    private const string ServiceName = "MicrosoftIdentityService";
    private readonly IOAuth2Service _auth2Service;

    public MicrosoftSSOAuthenticationProvider(IRecorder recorder, IHttpClientFactory clientFactory,
        JsonSerializerOptions jsonOptions, IConfigurationSettings settings) : this(
        new MicrosoftOAuth2HttpServiceClient(recorder, clientFactory, jsonOptions, settings))
    {
    }

    private MicrosoftSSOAuthenticationProvider(IOAuth2Service auth2Service)
    {
        _auth2Service = auth2Service;
    }

    public async Task<Result<SSOUserInfo, Error>> AuthenticateAsync(ICallerContext caller, string authCode,
        string? emailAddress, CancellationToken cancellationToken)
    {
        authCode.ThrowIfNotValuedParameter(nameof(authCode),
            Resources.AnySSOAuthenticationProvider_MissingRefreshToken);

        var retrievedTokens =
            await _auth2Service.ExchangeCodeForTokensAsync(caller,
                new OAuth2CodeTokenExchangeOptions(ServiceName, authCode), cancellationToken);
        if (retrievedTokens.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var tokens = retrievedTokens.Value;
        return tokens.ToSSoUserInfo();
    }

    public string ProviderName => SSOName;

    public async Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string refreshToken, CancellationToken cancellationToken)
    {
        refreshToken.ThrowIfNotValuedParameter(nameof(refreshToken),
            Resources.AnySSOAuthenticationProvider_MissingRefreshToken);

        var retrievedTokens =
            await _auth2Service.RefreshTokenAsync(caller,
                new OAuth2RefreshTokenOptions(ServiceName, refreshToken),
                cancellationToken);
        if (retrievedTokens.IsFailure)
        {
            return Error.NotAuthenticated();
        }

        var tokens = retrievedTokens.Value;

        var accessToken = tokens.Single(tok => tok.Type == TokenType.AccessToken);
        var refreshedToken = tokens.FirstOrDefault(tok => tok.Type == TokenType.RefreshToken);
        var idToken = tokens.FirstOrDefault(tok => tok.Type == TokenType.OtherToken);
        return new ProviderAuthenticationTokens
        {
            Provider = SSOName,
            AccessToken = new AuthenticationToken
            {
                ExpiresOn = accessToken.ExpiresOn,
                Type = TokenType.AccessToken,
                Value = accessToken.Value
            },
            RefreshToken = refreshedToken.Exists()
                ? new AuthenticationToken
                {
                    ExpiresOn = refreshedToken.ExpiresOn,
                    Type = TokenType.RefreshToken,
                    Value = refreshedToken.Value
                }
                : null,
            OtherTokens = idToken.Exists()
                ?
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = idToken.ExpiresOn,
                        Type = TokenType.OtherToken,
                        Value = idToken.Value
                    }
                ]
                : []
        };
    }
}

internal static class MicrosoftSSOAuthenticationProviderExtensions
{
    public static Result<SSOUserInfo, Error> ToSSoUserInfo(this List<AuthToken> tokens)
    {
        var idToken = tokens.FirstOrDefault(t => t.Type == TokenType.OtherToken);
        if (idToken.NotExists())
        {
            return Error.NotAuthenticated();
        }

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(idToken.Value).Claims.ToArray();
        var emailAddress = claims.Single(c => c.Type == ClaimTypes.Email).Value;
        var firstName = claims.Single(c => c.Type == ClaimTypes.GivenName).Value;
        var lastName = claims.Single(c => c.Type == "family_name").Value;
        var timezone =
            Timezones.FindOrDefault(claims.Single(c => c.Type == AuthenticationConstants.Claims.ForTimezone).Value);
        var country = CountryCodes.FindOrDefault(claims.Single(c => c.Type == ClaimTypes.Country).Value);

        return new SSOUserInfo(tokens, emailAddress, firstName, lastName, timezone, country);
    }
}