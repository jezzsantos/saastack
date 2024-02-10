using IdentityApplication;
using IdentityApplication.ApplicationServices;
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

    public async Task<ApiDeleteResult> Revoke(RevokeRefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens =
            await _authTokensApplication.RevokeRefreshTokenAsync(_contextFactory.Create(), request.RefreshToken,
                cancellationToken);

        return () => tokens.HandleApplicationResult();
    }

    public async Task<ApiPostResult<AccessTokens, RefreshTokenResponse>> Refresh(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens =
            await _authTokensApplication.RefreshTokenAsync(_contextFactory.Create(), request.RefreshToken,
                cancellationToken);

        return () => tokens.HandleApplicationResult<RefreshTokenResponse, AccessTokens>(x =>
            new PostResult<RefreshTokenResponse>(new RefreshTokenResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken,
                AccessTokenExpiresOnUtc = x.AccessTokenExpiresOn,
                RefreshTokenExpiresOnUtc = x.RefreshTokenExpiresOn
            }));
    }
}