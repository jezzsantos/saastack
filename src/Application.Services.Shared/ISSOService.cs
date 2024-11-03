using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service to access and manage the SSO AuthTokens for a user
/// </summary>
public interface ISSOService
{
    /// <summary>
    ///     Retrieves the list of tokens for the current user
    /// </summary>
    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves the list of tokens for the specified user
    /// </summary>
    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensOnBehalfOfUserAsync(ICallerContext caller,
        string userId, CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes the specified <see cref="refreshToken" /> for the current user
    /// </summary>
    Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string providerName, string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes the specified <see cref="refreshToken" /> for the specified user
    /// </summary>
    Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenOnBehalfOfUserAsync(ICallerContext caller,
        string userId, string providerName, string refreshToken, CancellationToken cancellationToken);
}