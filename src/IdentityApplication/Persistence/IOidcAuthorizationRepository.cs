using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IOidcAuthorizationRepository : IApplicationRepository
{
    Task<Result<Optional<OidcAuthorizationRoot>, Error>> FindByAuthorizationCodeAsync(Identifier clientId,
        string authorizationCode,
        CancellationToken cancellationToken);

    Task<Result<Optional<OidcAuthorizationRoot>, Error>> FindByClientAndUserAsync(Identifier clientId,
        Identifier userId, CancellationToken cancellationToken);

    Task<Result<OidcAuthorizationRoot, Error>> SaveAsync(OidcAuthorizationRoot authorization,
        CancellationToken cancellationToken);
}