using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using IdentityApplication.ApplicationServices;
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

    public async Task<Result<SSOAuthUserInfo, Error>> AuthenticateAsync(ICallerContext caller, string authCode,
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
    public static Result<SSOAuthUserInfo, Error> ToSSoUserInfo(this List<AuthToken> tokens)
    {
        var idToken = tokens.FirstOrDefault(t => t.Type == TokenType.OtherToken);
        if (idToken.NotExists())
        {
            return Error.NotAuthenticated();
        }

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(idToken.Value).Claims.ToArray();
        var oId = claims.Single(c => c.Type == MicrosoftIdentityClaims.ObjectId).Value;
        var emailAddress = claims.Single(c => c.Type == MicrosoftIdentityClaims.PreferredUserName).Value;
        var firstName = claims.Single(c => c.Type == MicrosoftIdentityClaims.GivenName).Value;
        var lastName = claims.Single(c => c.Type == MicrosoftIdentityClaims.FamilyName).Value;
        var timezone = Timezones.Gmt; // Note, we cannot reliably derive the users timezone from these claims!
        var countryCode = claims.FirstOrDefault(c => c.Type == MicrosoftIdentityClaims.Country)?.Value
                          ?? claims.FirstOrDefault(c => c.Type == MicrosoftIdentityClaims.TenantCountry)?.Value
                          ?? CountryCodes.Default.ToString();
        var country = CountryCodes.FindOrDefault(countryCode);

        return new SSOAuthUserInfo(tokens, oId, emailAddress, firstName, lastName, timezone, country);
    }

    /// <summary>
    ///     See the idToken claims:
    ///     <see href="https://learn.microsoft.com/en-us/entra/identity-platform/id-token-claims-reference#payload-claims" />
    ///     and optional <see href="https://learn.microsoft.com/en-us/entra/identity-platform/optional-claims-reference" />
    /// </summary>
    private static class MicrosoftIdentityClaims
    {
        public const string Country = "ctry";
        public const string FamilyName = "family_name";
        public const string GivenName = "given_name";
        public const string ObjectId = "oid";
        public const string PreferredUserName = "preferred_username";
        public const string TenantCountry = "tenant_ctry";
    }
}