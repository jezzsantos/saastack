using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines a service for creating OpenID Connect tokens
/// </summary>
public interface IOpenIdConnectTokenService
{
    /// <summary>
    ///     Creates an ID token for OpenID Connect
    /// </summary>
    Task<Result<string, Error>> CreateIdTokenAsync(EndUserWithMemberships user, string clientId, string? nonce = null);

    /// <summary>
    ///     Creates access and refresh tokens with optional ID token
    /// </summary>
    Task<Result<OpenIdConnectTokens, Error>> CreateTokensAsync(EndUserWithMemberships user, string clientId,
        bool includeIdToken = false, string? nonce = null);
}

/// <summary>
///     OpenID Connect token set
/// </summary>
public class OpenIdConnectTokens
{
    public required AuthenticationToken AccessToken { get; set; }

    public required AuthenticationToken RefreshToken { get; set; }

    public string? IdToken { get; set; }
}