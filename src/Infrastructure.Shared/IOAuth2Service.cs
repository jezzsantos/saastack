using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;

namespace Infrastructure.Shared;

/// <summary>
///     Defines a service for exchanging OAuth2 codes for tokens
/// </summary>
public interface IOAuth2Service
{
    Task<Result<List<AuthToken>, Error>> ExchangeCodeForTokensAsync(ICallerContext context,
        OAuth2CodeTokenExchangeOptions options,
        CancellationToken cancellationToken);
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