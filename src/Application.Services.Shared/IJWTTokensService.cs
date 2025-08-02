using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for issuing JWT tokens
/// </summary>
public interface IJWTTokensService
{
    /// <summary>
    ///     Issues a set of tokens for the specified user, including the access_token and refresh_token.
    ///     If <see cref="profile" /> is included then an additional id_token is also created using the scopes.
    ///     <see cref="additionalData" /> is added to both access_token and id_token tokens.
    /// </summary>
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