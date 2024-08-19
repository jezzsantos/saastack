using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;

namespace Application.Services.Shared;

/// <summary>
///     Defines a generic service for exchanging OAuth2 codes for tokens.
///     See RFC6749: <see href="https://datatracker.ietf.org/doc/html/rfc6749" />
/// </summary>
public interface IOAuth2Service
{
    /// <summary>
    ///     Exchanges an authorization code for a set of tokens, including the access_token, and possibly a refresh_token
    ///     See: <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-4.1" />
    /// </summary>
    Task<Result<List<AuthToken>, Error>> ExchangeCodeForTokensAsync(ICallerContext caller,
        OAuth2CodeTokenExchangeOptions options, CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes a refresh token
    ///     See: <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-6" />
    /// </summary>
    Task<Result<List<AuthToken>, Error>> RefreshTokenAsync(ICallerContext caller,
        OAuth2RefreshTokenOptions options, CancellationToken cancellationToken);
}

/// <summary>
///     Defines options for token exchange
/// </summary>
public class OAuth2CodeTokenExchangeOptions
{
    public OAuth2CodeTokenExchangeOptions(string serviceName, string code, string? codeVerifier = null,
        string? scope = null)
    {
        serviceName.ThrowIfNotValuedParameter(nameof(serviceName));
        code.ThrowIfNotValuedParameter(nameof(code));
        ServiceName = serviceName;
        Code = code;
        CodeVerifier = codeVerifier;
        Scope = scope;
    }

    public string Code { get; }

    public string? CodeVerifier { get; }

    public string? Scope { get; }

    public string ServiceName { get; set; }
}

/// <summary>
///     Defines options for refreshing tokens
/// </summary>
public class OAuth2RefreshTokenOptions
{
    public OAuth2RefreshTokenOptions(string serviceName, string refreshToken)
    {
        serviceName.ThrowIfNotValuedParameter(nameof(serviceName));
        refreshToken.ThrowIfNotValuedParameter(nameof(refreshToken));
        ServiceName = serviceName;
        RefreshToken = refreshToken;
    }

    public string RefreshToken { get; }

    public string ServiceName { get; set; }
}