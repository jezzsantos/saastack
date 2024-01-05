using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IJWTTokensService
{
    Task<Result<AccessTokens, Error>> IssueTokensAsync(EndUser user);
}

public struct AccessTokens
{
    public AccessTokens(string accessToken, string refreshToken, DateTime expiresOn)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresOn = expiresOn;
    }

    public string AccessToken { get; }

    public string RefreshToken { get; }

    public DateTime ExpiresOn { get; }
}