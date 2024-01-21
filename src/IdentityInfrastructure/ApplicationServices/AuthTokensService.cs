using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using IdentityApplication;
using IdentityApplication.ApplicationServices;

namespace IdentityInfrastructure.ApplicationServices;

public class AuthTokensService : IAuthTokensService
{
    private readonly IAuthTokensApplication _authTokensApplication;

    public AuthTokensService(IAuthTokensApplication authTokensApplication)
    {
        _authTokensApplication = authTokensApplication;
    }

    public async Task<Result<AccessTokens, Error>> IssueTokensAsync(ICallerContext context, EndUserWithMemberships user,
        CancellationToken cancellationToken)
    {
        return await _authTokensApplication.IssueTokensAsync(context, user, cancellationToken);
    }
}