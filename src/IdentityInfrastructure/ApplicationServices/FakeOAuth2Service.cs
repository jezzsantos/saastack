#if TESTINGONLY
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using IdentityApplication.ApplicationServices;
using Infrastructure.Interfaces;

namespace IdentityInfrastructure.ApplicationServices;

/// <summary>
///     Provides a fake example OAuth2 service that returns a set of OAuth tokens
/// </summary>
public class FakeOAuth2Service : IOAuth2Service
{
    public Task<Result<List<AuthToken>, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        OAuth2CodeTokenExchangeOptions options, CancellationToken cancellationToken)
    {
        if (options.Code != "1234567890")
        {
            return Task.FromResult<Result<List<AuthToken>, Error>>(Error.RuleViolation());
        }

        return Task.FromResult<Result<List<AuthToken>, Error>>(new List<AuthToken>
        {
            CreateAccessToken(options)
        });
    }

    public Task<Result<List<AuthToken>, Error>> RefreshTokenAsync(ICallerContext caller,
        OAuth2RefreshTokenOptions options, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<List<AuthToken>, Error>>(Error.PreconditionViolation("Not Supported"));
    }

    public static Result<ProviderAuthenticationTokens, Error> GetProviderTokensFromTokens(string providerName,
        List<AuthToken> tokens)
    {
        var accessToken = tokens.Single(tok => tok.Type == TokenType.AccessToken);
        return new ProviderAuthenticationTokens
        {
            Provider = providerName,
            AccessToken = new AuthenticationToken
            {
                ExpiresOn = accessToken.ExpiresOn,
                Type = TokenType.AccessToken,
                Value = accessToken.Value
            },
            RefreshToken = null,
            OtherTokens = []
        };
    }

    public static SSOAuthUserInfo GetUserInfoFromTokens(List<AuthToken> tokens)
    {
        var accessToken = tokens.Single(tok => tok.Type == TokenType.AccessToken).Value;
        var uid = Guid.NewGuid().ToString("N");
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims.ToArray();
        var emailAddress = claims.Single(c => c.Type == ClaimTypes.Email).Value;
        var firstName = claims.Single(c => c.Type == ClaimTypes.GivenName).Value;
        var lastName = claims.Single(c => c.Type == ClaimTypes.Surname).Value;
        var timezone =
            Timezones.FindOrDefault(claims.Single(c => c.Type == AuthenticationConstants.Claims.ForTimezone).Value);
        var country = CountryCodes.FindOrDefault(claims.Single(c => c.Type == ClaimTypes.Country).Value);

        return new SSOAuthUserInfo(tokens, uid, emailAddress, firstName, lastName, timezone, country);
    }

    private static AuthToken CreateAccessToken(OAuth2CodeTokenExchangeOptions options)
    {
        var expiresOn = DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.Email, options.CodeVerifier!),
                new Claim(ClaimTypes.GivenName, options.CodeVerifier!),
                new Claim(ClaimTypes.Surname, "asurname"),
                new Claim(AuthenticationConstants.Claims.ForTimezone, Timezones.Default.ToString()),
                new Claim(ClaimTypes.Country, CountryCodes.Default.ToString())
            ], expires: expiresOn,
            issuer: options.ServiceName
        ));

        return new AuthToken(TokenType.AccessToken, accessToken, expiresOn);
    }
}
#endif