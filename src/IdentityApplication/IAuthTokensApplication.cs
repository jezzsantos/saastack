using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using IdentityApplication.ApplicationServices;

namespace IdentityApplication;

public interface IAuthTokensApplication
{
    Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext context, EndUser user,
        CancellationToken cancellationToken);

    Task<Result<AccessTokens, Error>> RefreshTokenAsync(ICallerContext context, string refreshToken,
        CancellationToken cancellationToken);
}