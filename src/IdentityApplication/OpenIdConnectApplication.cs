using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using OAuth2GrantType = Application.Resources.Shared.OAuth2GrantType;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;

namespace IdentityApplication;

public class OpenIdConnectApplication : IOpenIdConnectApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public OpenIdConnectApplication(IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<OpenIdConnectAuthorization, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, OAuth2ResponseType responseType, string scope, string? state, string? nonce,
        string? codeChallenge, OpenIdConnectCodeChallengeMethod? codeChallengeMethod,
        CancellationToken cancellationToken)
    {
        var userId = caller.CallerId;
        return await _identityServerProvider.OpenIdConnectService.AuthorizeAsync(caller, clientId, userId, redirectUri,
            responseType, scope, state, nonce, codeChallenge, codeChallengeMethod, cancellationToken);
    }

    public async Task<Result<OpenIdConnectTokens, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        OAuth2GrantType grantType, string clientId, string clientSecret, string code, string redirectUri,
        string? codeVerifier, string refreshToken, string? scope, CancellationToken cancellationToken)
    {
        if (grantType == OAuth2GrantType.Authorization_Code)
        {
            return await _identityServerProvider.OpenIdConnectService.ExchangeCodeForTokensAsync(
                caller, clientId, clientSecret, code, redirectUri, codeVerifier, cancellationToken);
        }

        if (grantType == OAuth2GrantType.Refresh_Token)
        {
            return await _identityServerProvider.OpenIdConnectService.RefreshTokenAsync(
                caller, clientId, clientSecret, refreshToken, scope, cancellationToken);
        }

        return Error.Validation(Resources.OpenIdConnectApplication_UnsupportedGrantType.Format(grantType));
    }

    public async Task<Result<OpenIdConnectDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OpenIdConnectService.GetDiscoveryDocumentAsync(caller, cancellationToken);
    }

    public async Task<Result<JsonWebKeySet, Error>> GetJsonWebKeySetAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OpenIdConnectService.GetJsonWebKeySetAsync(caller, cancellationToken);
    }

    public async Task<Result<OpenIdConnectUserInfo, Error>> GetUserInfoForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OpenIdConnectService.GetUserInfoAsync(caller, caller.ToCallerId(),
            cancellationToken);
    }
}