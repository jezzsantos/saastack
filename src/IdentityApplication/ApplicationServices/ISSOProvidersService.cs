using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Domain.Common.ValueObjects;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines a service for accessing registered <see cref="ISSOAuthenticationProvider" />s
/// </summary>
public interface ISSOProvidersService
{
    Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindByProviderNameAsync(string providerName,
        CancellationToken cancellationToken);

    Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindByUserIdAsync(ICallerContext caller,
        Identifier userId, string providerName,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ProviderAuthenticationTokens>, Error>> GetTokensAsync(ICallerContext caller,
        Identifier userId, CancellationToken cancellationToken);

    Task<Result<Error>> SaveUserInfoAsync(ICallerContext caller, string providerName, Identifier userId,
        SSOUserInfo userInfo,
        CancellationToken cancellationToken);

    Task<Result<Error>> SaveUserTokensAsync(ICallerContext caller, string providerName, Identifier userId,
        ProviderAuthenticationTokens tokens,
        CancellationToken cancellationToken);
}