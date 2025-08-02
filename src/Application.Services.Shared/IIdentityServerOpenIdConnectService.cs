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
    Task<Result<OpenIdConnectAuthorization, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string userId,
        string redirectUri, OAuth2ResponseType responseType, string scope, string? state, string? nonce,
        string? codeChallenge,
        OpenIdConnectCodeChallengeMethod? codeChallengeMethod, CancellationToken cancellationToken);

    /// <summary>
    ///     Exchanges authorization code for tokens or refreshes tokens
    /// </summary>
    Task<Result<OpenIdConnectTokens, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        string clientId,
        string clientSecret, string code, string redirectUri,
        string? codeVerifier,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the OpenID Connect discovery document
    /// </summary>
    Task<Result<OpenIdConnectDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the JSON Web Key Set for token verification
    /// </summary>
    Task<Result<JsonWebKeySet, Error>>
        GetJsonWebKeySetAsync(ICallerContext caller, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets user information for the user
    /// </summary>
    Task<Result<OpenIdConnectUserInfo, Error>> GetUserInfoAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes access tokens using a refresh token
    /// </summary>
    Task<Result<OpenIdConnectTokens, Error>> RefreshTokenAsync(ICallerContext caller, string clientId,
        string clientSecret, string refreshToken, string? scope, CancellationToken cancellationToken);
}