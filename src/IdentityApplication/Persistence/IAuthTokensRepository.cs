using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IAuthTokensRepository : IApplicationRepository
{
    Task<Result<Optional<AuthTokensRoot>, Error>> FindByRefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken);

    Task<Result<Optional<AuthTokensRoot>, Error>> FindByUserIdAsync(Identifier userId,
        CancellationToken cancellationToken);

    Task<Result<AuthTokensRoot, Error>> SaveAsync(AuthTokensRoot tokens, CancellationToken cancellationToken);
}