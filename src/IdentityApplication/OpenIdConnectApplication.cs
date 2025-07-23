using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using IdentityApplication.ApplicationServices;
using IdentityDomain;

namespace IdentityApplication;

public class OpenIdConnectApplication : IOpenIdConnectApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public OpenIdConnectApplication(
        IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<OidcAuthorizationResponse, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, string responseType, string scope, string? state, string? nonce, string? codeChallenge,
        string? codeChallengeMethod, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OidcService.AuthorizeAsync(caller, clientId, redirectUri, responseType,
            scope, state, nonce, codeChallenge, codeChallengeMethod, cancellationToken);
    }

    public async Task<Result<OidcTokenResponse, Error>> CreateTokenAsync(ICallerContext caller, string grantType,
        string clientId, string clientSecret, string code, string? codeVerifier,
        string redirectUri, string refreshToken, string? scope, CancellationToken cancellationToken)
    {
        if (grantType.EqualsIgnoreCase(OpenIdConnectConstants.GrantTypes.AuthorizationCode))
        {
            return await _identityServerProvider.OidcService.ExchangeCodeForTokensAsync(
                caller, clientId, clientSecret, code, redirectUri, codeVerifier, cancellationToken);
        }

        if (grantType.EqualsIgnoreCase(OpenIdConnectConstants.GrantTypes.RefreshToken))
        {
            return await _identityServerProvider.OidcService.RefreshTokenAsync(
                caller, clientId, clientSecret, refreshToken, scope, cancellationToken);
        }

        return Error.Validation(Resources.OpenIdConnectApplication_UnsupportedGrantType.Format(grantType));
    }

    public async Task<Result<OidcDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OidcService.GetDiscoveryDocumentAsync(caller, cancellationToken);
    }

    public async Task<Result<JsonWebKeySet, Error>> GetJsonWebKeySetAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OidcService.GetJsonWebKeySetAsync(caller, cancellationToken);
    }

    public async Task<Result<OidcUserInfoResponse, Error>> GetUserInfoForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.OidcService.GetUserInfoForCallerAsync(caller, cancellationToken);
    }
}