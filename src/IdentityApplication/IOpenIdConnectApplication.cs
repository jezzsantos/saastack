using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IOpenIdConnectApplication
{
    Task<Result<OpenIdConnectAuthorization, Error>> AuthorizeAsync(ICallerContext caller, string clientId,
        string redirectUri, OAuth2ResponseType responseType, string scope, string? state, string? nonce,
        string? codeChallenge, OpenIdConnectCodeChallengeMethod? codeChallengeMethod,
        CancellationToken cancellationToken);

    Task<Result<OpenIdConnectTokens, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        OAuth2GrantType grantType, string clientId, string clientSecret, string code,
        string redirectUri, string? codeVerifier,
        string refreshToken, string? scope, CancellationToken cancellationToken);

    Task<Result<OpenIdConnectDiscoveryDocument, Error>> GetDiscoveryDocumentAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    Task<Result<JsonWebKeySet, Error>>
        GetJsonWebKeySetAsync(ICallerContext caller, CancellationToken cancellationToken);

    Task<Result<OpenIdConnectUserInfo, Error>> GetUserInfoForCallerAsync(ICallerContext caller,
        CancellationToken cancellationToken);
}