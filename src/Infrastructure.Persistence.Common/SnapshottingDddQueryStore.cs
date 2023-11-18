using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to individual/collection of <see cref="IDehydratableEntity" /> DDD aggregates/entities
///     for [CQRS]
///     queries that use snapshotting persistence
/// </summary>
public sealed class
    SnapshottingDddQueryStore<TAggregateRootOrEntity> : ISnapshottingDddQueryStore<TAggregateRootOrEntity>
    where TAggregateRootOrEntity : IQueryableEntity, new()
{
    private readonly string _containerName;
    private readonly IDataStore _dataStore;
    private readonly IDomainFactory _domainFactory;
    private readonly IRecorder _recorder;

    public SnapshottingDddQueryStore(IRecorder recorder, IDomainFactory domainFactory, IDataStore dataStore)
    {
        _recorder = recorder;
        _dataStore = dataStore;
        _domainFactory = domainFactory;
        _containerName = typeof(TAggregateRootOrEntity).GetEntityNameSafe();
    }

    public async Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken)
    {
        return await _dataStore.CountAsync(_containerName, cancellationToken);
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _dataStore.DestroyAllAsync(_containerName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All entities were deleted from the {Store} store", _dataStore.GetType().Name);
        }

        return deleted;
    }

    public async Task<Result<Optional<TEntityWithId>, Error>> GetAsync<TEntityWithId>(Identifier id,
        bool errorIfNotFound = true, bool includeDeleted = false,
        CancellationToken cancellationToken = default)
        where TEntityWithId : IQueryableEntity, IHasIdentity, new()
    {
        var retrieved = await _dataStore.RetrieveAsync(_containerName, id,
            PersistedEntityMetadata.FromType<TAggregateRootOrEntity>(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TEntityWithId>.None;
        }

        var entity = retrieved.Value.Value;
        if (entity.IsDeleted.ValueOrDefault && !includeDeleted)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TEntityWithId>.None;
        }

        _recorder.TraceDebug(null, "Entity {Id} was retrieved from the {Store} store", id, _dataStore.GetType().Name);
        return new Result<Optional<TEntityWithId>, Error>(entity.ToQueryEntity<TEntityWithId>(_domainFactory));
    }

    public async Task<Result<QueryResults<TAggregateRootOrEntity>, Error>> QueryAsync(
        QueryClause<TAggregateRootOrEntity> query,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (query.Options.IsEmpty)
        {
            _recorder.TraceDebug(null, "No entities were retrieved from the store, the query is empty");

            return new QueryResults<TAggregateRootOrEntity>(new List<TAggregateRootOrEntity>());
        }

        var queryResults = await _dataStore.QueryAsync(_containerName, query,
            PersistedEntityMetadata.FromType<TAggregateRootOrEntity>(),
            cancellationToken);
        if (!queryResults.IsSuccessful)
        {
            return queryResults.Error;
        }

        var entities = queryResults.Value;

        entities = entities
            .Where(e => !e.IsDeleted.ValueOrDefault || includeDeleted)
            .ToList();

        _recorder.TraceDebug(null, "{Count} entities were retrieved from the {Store} store", entities.Count,
            _dataStore.GetType().Name);
        return new QueryResults<TAggregateRootOrEntity>(entities.ConvertAll(x =>
            x.ToDomainEntity<TAggregateRootOrEntity>(_domainFactory)));
    }
}