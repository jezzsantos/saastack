using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines a service for accessing the registered <see cref="ISSOAuthenticationProvider" />s
/// </summary>
public interface ISSOProvidersService
{
    Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindProviderByNameAsync(string providerName,
        CancellationToken cancellationToken);

    Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindProviderByUserIdAsync(ICallerContext caller,
        string userId, string providerName, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensOnBehalfOfUserAsync(ICallerContext caller,
        string userId, CancellationToken cancellationToken);

    Task<Result<Error>> SaveInfoOnBehalfOfUserAsync(ICallerContext caller, string providerName, string userId,
        SSOUserInfo userInfo, CancellationToken cancellationToken);

    Task<Result<Error>> SaveTokensOnBehalfOfUserAsync(ICallerContext caller, string providerName, string userId,
        ProviderAuthenticationTokens tokens, CancellationToken cancellationToken);
}