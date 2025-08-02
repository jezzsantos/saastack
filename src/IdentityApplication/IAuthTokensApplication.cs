using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IAuthTokensApplication
{
    Task<Result<AuthenticateTokens, Error>> IssueTokensAsync(ICallerContext caller, string userId,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData, CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext caller, string refreshToken,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData, CancellationToken cancellationToken);

    Task<Result<Error>> RevokeRefreshTokenAsync(ICallerContext caller, string refreshToken,
        CancellationToken cancellationToken);
}