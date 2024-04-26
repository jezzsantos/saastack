using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using IdentityApplication.ApplicationServices;

namespace IdentityApplication;

public interface IAuthTokensApplication
{
    Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext caller, EndUserWithMemberships user,
        CancellationToken cancellationToken);

    Task<Result<AuthenticateTokens, Error>> RefreshTokenAsync(ICallerContext caller, string refreshToken,
        CancellationToken cancellationToken);

    Task<Result<Error>> RevokeRefreshTokenAsync(ICallerContext caller, string refreshToken,
        CancellationToken cancellationToken);
}