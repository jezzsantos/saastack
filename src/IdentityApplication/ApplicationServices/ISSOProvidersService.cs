using Common;
using Domain.Common.ValueObjects;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines a service for accessing registered <see cref="ISSOAuthenticationProvider" />s
/// </summary>
public interface ISSOProvidersService
{
    Task<Result<Optional<ISSOAuthenticationProvider>, Error>> FindByNameAsync(string name,
        CancellationToken cancellationToken);

    Task<Result<Error>> SaveUserInfoAsync(string providerName, Identifier userId, SSOUserInfo userInfo,
        CancellationToken cancellationToken);
}