using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IAuthTokensService
{
    Task<Result<AuthenticateTokens, Error>> IssueTokensAsync(ICallerContext caller, string userId,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData,
        CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> RefreshTokensAsync(ICallerContext caller, string refreshToken,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData,
        CancellationToken cancellationToken);
}