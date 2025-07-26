using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for managing OpenID Connect for an identity server
/// </summary>
public interface IIdentityServerOpenIdConnectService
{
    /// <summary>
    ///     Handles the authorization request for the authorization code flow, and returns an authorization code.
    ///     If not authenticated yet (via Credentials or SSO), redirect to a login URI.
    ///     If the user has not been consented to use this client, redirect to a consent URI.
    /// </summary>
    Task<Result<OidcAuthorizationResponse, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, string responseType, string scope, string? state, string? nonce, string? codeChallenge,
        string? codeChallengeMethod, CancellationToken cancellationToken);

    /// <summary>
    ///     Exchanges authorization code for tokens or refreshes tokens
    /// </summary>
    Task<Result<OidcTokenResponse, Error>> ExchangeCodeForTokensAsync(ICallerContext caller, string clientId,
        string clientSecret, string code, string? codeVerifier, string redirectUri,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the OpenID Connect discovery document
    /// </summary>
    Task<Result<OidcDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the JSON Web Key Set for token verification
    /// </summary>
    Task<Result<JsonWebKeySet, Error>>
        GetJsonWebKeySetAsync(ICallerContext caller, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets user information for the user
    /// </summary>
    Task<Result<OidcUserInfoResponse, Error>> GetUserInfoAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes access tokens using a refresh token
    /// </summary>
    Task<Result<OidcTokenResponse, Error>> RefreshTokenAsync(ICallerContext caller, string clientId,
        string clientSecret, string refreshToken, string? scope, CancellationToken cancellationToken);
}