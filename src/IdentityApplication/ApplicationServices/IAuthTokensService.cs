using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IAuthTokensService
{
    Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext context, EndUserWithMemberships user,
        CancellationToken cancellationToken);
}