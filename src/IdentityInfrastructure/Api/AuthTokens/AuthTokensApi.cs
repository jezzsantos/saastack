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
    private readonly ICallerContextFactory _contextFactory;

    public AuthTokensApi(ICallerContextFactory contextFactory,
        IAuthTokensApplication authTokensApplication)
    {
        _contextFactory = contextFactory;
        _authTokensApplication = authTokensApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, RefreshTokenResponse>> Refresh(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens =
            await _authTokensApplication.RefreshTokenAsync(_contextFactory.Create(), request.RefreshToken,
                cancellationToken);

        return () => tokens.HandleApplicationResult<RefreshTokenResponse, AuthenticateTokens>(tok =>
            new PostResult<RefreshTokenResponse>(new RefreshTokenResponse
            {
                Tokens = tok
            }));
    }

    public async Task<ApiDeleteResult> Revoke(RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens =
            await _authTokensApplication.RevokeRefreshTokenAsync(_contextFactory.Create(), request.RefreshToken,
                cancellationToken);

        return () => tokens.HandleApplicationResult();
    }
}