using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service for managing Single Sign On for an identity server
/// </summary>
public interface IIdentityServerSingleSignOnService
{
    /// <summary>
    ///     Authenticates the user with the specified provider
    /// </summary>
    Task<Result<AuthenticateTokens, Error>> AuthenticateAsync(ICallerContext caller, string? invitationToken,
        string providerName, string authCode, string? codeVerifier, string? username, bool? termsAndConditionsAccepted,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the list of tokens for the specified user
    /// </summary>
    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensForUserAsync(ICallerContext caller,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Refreshes the specified refresh token for the specified provider and user
    /// </summary>
    Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenForUserAsync(ICallerContext caller, string userId,
        string providerName, string refreshToken, CancellationToken cancellationToken);
}