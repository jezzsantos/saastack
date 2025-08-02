using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IJWTTokensService
{
    Task<Result<AccessTokens, Error>> IssueTokensAsync(EndUserWithMemberships user, UserProfile? profile,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData);
}

public struct AccessTokens
{
    public AccessTokens(string accessToken, DateTime accessTokenExpiresOn, string refreshToken,
        DateTime refreshTokenExpiresOn, string? idToken = null, DateTime? idTokenExpiresOn = null)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        IdToken = idToken;
        AccessTokenExpiresOn = accessTokenExpiresOn;
        RefreshTokenExpiresOn = refreshTokenExpiresOn;
        IdTokenExpiresOn = idTokenExpiresOn;
    }

    public string AccessToken { get; }

    public string RefreshToken { get; }

    public string? IdToken { get; }

    public DateTime AccessTokenExpiresOn { get; }

    public DateTime RefreshTokenExpiresOn { get; }

    public DateTime? IdTokenExpiresOn { get; }
}