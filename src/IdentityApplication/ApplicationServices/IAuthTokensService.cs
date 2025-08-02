using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IAuthTokensService
{
    Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext caller, EndUserWithMemberships user,
        CancellationToken cancellationToken);

    Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext caller, EndUserWithMemberships user,
        UserProfile profile, IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData,
        CancellationToken cancellationToken);
}