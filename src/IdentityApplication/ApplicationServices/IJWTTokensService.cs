using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IJWTTokensService
{
    Task<Result<AccessTokens, Error>> IssueTokensAsync(EndUserWithMemberships user);
}

public struct AccessTokens
{
    public AccessTokens(string accessToken, DateTime accessTokenExpiresOn, string refreshToken,
        DateTime refreshTokenExpiresOn)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        AccessTokenExpiresOn = accessTokenExpiresOn;
        RefreshTokenExpiresOn = refreshTokenExpiresOn;
    }

    public string AccessToken { get; }

    public string RefreshToken { get; }

    public DateTime AccessTokenExpiresOn { get; }

    public DateTime RefreshTokenExpiresOn { get; }
}