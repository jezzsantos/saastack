using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.AuthTokens;

public class AuthTokensApi : IWebApiService
{
    private readonly IAuthTokensApplication _authTokensApplication;
    private readonly ICallerContextFactory _callerFactory;

    public AuthTokensApi(ICallerContextFactory callerFactory,
        IAuthTokensApplication authTokensApplication)
    {
        _callerFactory = callerFactory;
        _authTokensApplication = authTokensApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, RefreshTokenResponse>> Refresh(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens =
            await _authTokensApplication.RefreshTokenAsync(_callerFactory.Create(), request.RefreshToken,
                cancellationToken);

        return () => tokens.HandleApplicationResult<AuthenticateTokens, RefreshTokenResponse>(tok =>
            new PostResult<RefreshTokenResponse>(new RefreshTokenResponse
            {
                Tokens = tok
            }));
    }

    public async Task<ApiDeleteResult> Revoke(RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens =
            await _authTokensApplication.RevokeRefreshTokenAsync(_callerFactory.Create(), request.RefreshToken,
                cancellationToken);

        return () => tokens.HandleApplicationResult();
    }
}