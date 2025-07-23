using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines the OpenID Connect application service for handling OIDC flows
/// </summary>
public interface IOpenIdConnectApplication
{
    /// <summary>
    ///     Handles the authorization request (authorization code flow)
    /// </summary>
    Task<Result<OidcAuthorizationResponse, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri,
        string responseType, string scope, string? state, string? nonce, string? codeChallenge,
        string? codeChallengeMethod, CancellationToken cancellationToken);

    /// <summary>
    ///     Exchanges authorization code for tokens
    /// </summary>
    Task<Result<OidcTokenResponse, Error>> ExchangeCodeForTokensAsync(ICallerContext caller, string clientId,
        string clientSecret, string code, string redirectUri, string? codeVerifier,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes tokens using refresh token
    /// </summary>
    Task<Result<OidcTokenResponse, Error>> RefreshTokenAsync(ICallerContext caller, string clientId,
        string clientSecret,
        string refreshToken, string? scope, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets user information for the current authenticated user
    /// </summary>
    Task<Result<OidcUserInfoResponse, Error>> GetUserInfoForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the OpenID Connect discovery document
    /// </summary>
    Task<Result<OidcDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the JSON Web Key Set (JWKS)
    /// </summary>
    Task<Result<JsonWebKeySet, Error>>
        GetJsonWebKeySetAsync(ICallerContext caller, CancellationToken cancellationToken);
}