using Application.Persistence.Interfaces;
using Common;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IAPIKeysRepository : IApplicationRepository
{
    Task<Result<Optional<APIKeyRoot>, Error>> FindByAPIKeyTokenAsync(string keyToken,
        CancellationToken cancellationToken);

    Task<Result<APIKeyRoot, Error>> SaveAsync(APIKeyRoot apiKey, CancellationToken cancellationToken);
}