using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using IdentityApplication.Persistence;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace IdentityInfrastructure.Persistence;

public class APIKeysRepository : IAPIKeysRepository
{
    private readonly SnapshottingQueryStore<APIKey> _apiKeyQueries;
    private readonly IEventSourcingDddCommandStore<APIKeyRoot> _apiKeys;

    public APIKeysRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<APIKeyRoot> apiKeyStore, IDataStore store)
    {
        _apiKeyQueries = new SnapshottingQueryStore<APIKey>(recorder, domainFactory, store);
        _apiKeys = apiKeyStore;
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _apiKeyQueries.DestroyAllAsync(cancellationToken),
            _apiKeys.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<APIKeyRoot, Error>> SaveAsync(APIKeyRoot apiKey, CancellationToken cancellationToken)
    {
        await _apiKeys.SaveAsync(apiKey, cancellationToken);

        return apiKey;
    }

    public async Task<Result<Optional<APIKeyRoot>, Error>> FindByAPIKeyTokenAsync(string keyToken,
        CancellationToken cancellationToken)
    {
        var query = Query.From<APIKey>()
            .Where<string>(at => at.KeyToken, ConditionOperator.EqualTo, keyToken);
        return await FindFirstByQueryAsync(query, cancellationToken);
    }

    private async Task<Result<Optional<APIKeyRoot>, Error>> FindFirstByQueryAsync(QueryClause<APIKey> query,
        CancellationToken cancellationToken)
    {
        var queried = await _apiKeyQueries.QueryAsync(query, false, cancellationToken);
        if (!queried.IsSuccessful)
        {
            return queried.Error;
        }

        var matching = queried.Value.Results.FirstOrDefault();
        if (matching.NotExists())
        {
            return Optional<APIKeyRoot>.None;
        }

        var tokens = await _apiKeys.LoadAsync(matching.Id.Value.ToId(), cancellationToken);
        if (!tokens.IsSuccessful)
        {
            return tokens.Error;
        }

        return tokens.Value.ToOptional();
    }
}