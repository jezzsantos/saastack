using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface ISSOUsersRepository : IApplicationRepository
{
    Task<Result<Optional<SSOUserRoot>, Error>> FindByProviderUIdAsync(string providerName, string providerUId,
        CancellationToken cancellationToken);

    Task<Result<Optional<SSOUserRoot>, Error>> FindByUserIdAsync(string providerName, Identifier userId,
        CancellationToken cancellationToken);

    Task<Result<Optional<ProviderAuthTokensRoot>, Error>> FindProviderTokensByUserIdAndProviderAsync(
        string providerName, Identifier userId, CancellationToken cancellationToken);

    Task<Result<SSOUserRoot, Error>> SaveAsync(SSOUserRoot user, CancellationToken cancellationToken);

    Task<Result<ProviderAuthTokensRoot, Error>> SaveAsync(ProviderAuthTokensRoot authTokens,
        CancellationToken cancellationToken);
}