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

    public async Task<Result<AuthenticateTokens, Error>> IssueTokensAsync(ICallerContext caller, string userId,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData, CancellationToken cancellationToken)
    {
        return await _authTokensApplication.IssueTokensAsync(caller, userId, scopes, additionalData, cancellationToken);
    }

    public async Task<Result<AuthenticateTokens, Error>> RefreshTokensAsync(ICallerContext caller, string refreshToken,
        IReadOnlyList<string>? scopes, Dictionary<string, object>? additionalData, CancellationToken cancellationToken)
    {
        return await _authTokensApplication.RefreshTokenAsync(caller, refreshToken, scopes, additionalData,
            cancellationToken);
    }
}