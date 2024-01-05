using Application.Interfaces;
using IdentityApplication;
using IdentityApplication.ApplicationServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.AuthTokens;

public class AuthTokensApi : IWebApiService
{
    private readonly IAuthTokensApplication _authTokensApplication;

    private readonly ICallerContext _context;

    public AuthTokensApi(ICallerContext context,
        IAuthTokensApplication authTokensApplication)
    {
        _context = context;
        _authTokensApplication = authTokensApplication;
    }

    public async Task<ApiPostResult<AccessTokens, RefreshTokenResponse>> Refresh(RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _authTokensApplication.RefreshTokenAsync(_context, request.RefreshToken, cancellationToken);

        return () => tokens.HandleApplicationResult<RefreshTokenResponse, AccessTokens>(x =>
            new PostResult<RefreshTokenResponse>(new RefreshTokenResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken,
                ExpiresOnUtc = x.ExpiresOn
            }));
    }
}