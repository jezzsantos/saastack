using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IOpenIdConnectApplication
{
    Task<Result<OidcAuthorizationResponse, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri,
        string responseType, string scope, string? state, string? nonce, string? codeChallenge,
        string? codeChallengeMethod, CancellationToken cancellationToken);

    Task<Result<OidcTokenResponse, Error>> CreateTokenAsync(ICallerContext caller, string grantType, string clientId,
        string clientSecret,
        string code, string? codeVerifier, string redirectUri, string refreshToken, string? scope,
        CancellationToken cancellationToken);

    Task<Result<OidcDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    Task<Result<JsonWebKeySet, Error>>
        GetJsonWebKeySetAsync(ICallerContext caller, CancellationToken cancellationToken);

    Task<Result<OidcUserInfoResponse, Error>> GetUserInfoForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken);
}