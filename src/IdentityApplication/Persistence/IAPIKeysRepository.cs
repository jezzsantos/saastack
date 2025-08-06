using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;

namespace IdentityApplication.Persistence;

public interface IAPIKeysRepository : IApplicationRepository
{
    Task<Result<Optional<APIKeyRoot>, Error>> FindByAPIKeyTokenAsync(string keyToken,
        CancellationToken cancellationToken);

    Task<Result<APIKeyRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<APIKeyRoot, Error>> SaveAsync(APIKeyRoot apiKey, CancellationToken cancellationToken);

    Task<Result<QueryResults<APIKeyAuth>, Error>> SearchAllForUserAsync(Identifier userId, SearchOptions options,
        CancellationToken cancellationToken);

    Task<Result<QueryResults<APIKeyAuth>, Error>>
        SearchAllUnexpiredForUserAsync(Identifier userId, CancellationToken cancellationToken);
}