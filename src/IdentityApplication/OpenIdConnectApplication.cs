using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using IdentityDomain;

namespace IdentityApplication;

public class OpenIdConnectApplication : IOpenIdConnectApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public OpenIdConnectApplication(IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<OidcAuthorizationResponse, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, string responseType, string scope, string? state, string? nonce, string? codeChallenge,
        string? codeChallengeMethod, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OpenIdConnectService.AuthorizeAsync(caller, clientId, redirectUri,
            responseType,
            scope, state, nonce, codeChallenge, codeChallengeMethod, cancellationToken);
    }

    public async Task<Result<OidcTokenResponse, Error>> CreateTokenAsync(ICallerContext caller, string grantType,
        string clientId, string clientSecret, string code, string? codeVerifier,
        string redirectUri, string refreshToken, string? scope, CancellationToken cancellationToken)
    {
        if (grantType.EqualsIgnoreCase(OAuth2Constants.GrantTypes.AuthorizationCode))
        {
            return await _identityServerProvider.OpenIdConnectService.ExchangeCodeForTokensAsync(
                caller, clientId, clientSecret, code, codeVerifier, redirectUri, cancellationToken);
        }

        if (grantType.EqualsIgnoreCase(OAuth2Constants.GrantTypes.RefreshToken))
        {
            return await _identityServerProvider.OpenIdConnectService.RefreshTokenAsync(
                caller, clientId, clientSecret, refreshToken, scope, cancellationToken);
        }

        return Error.Validation(Resources.OpenIdConnectApplication_UnsupportedGrantType.Format(grantType));
    }

    public async Task<Result<OidcDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OpenIdConnectService.GetDiscoveryDocumentAsync(caller, cancellationToken);
    }

    public async Task<Result<JsonWebKeySet, Error>> GetJsonWebKeySetAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OpenIdConnectService.GetJsonWebKeySetAsync(caller, cancellationToken);
    }

    public async Task<Result<OidcUserInfoResponse, Error>> GetUserInfoForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OpenIdConnectService.GetUserInfoAsync(caller, caller.ToCallerId(),
            cancellationToken);
    }
}