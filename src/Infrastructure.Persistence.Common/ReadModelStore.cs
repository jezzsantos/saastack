using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access for [CQRS] commands and queries that use snapshotting for any kind of read model
/// </summary>
public class ReadModelStore<TReadModelEntity> : IReadModelStore<TReadModelEntity>
    where TReadModelEntity : IReadModelEntity, new()
{
    private readonly IDataStore _dataStore;
    private readonly IDomainFactory _domainFactory;
    private readonly IRecorder _recorder;

    public ReadModelStore(IRecorder recorder, IDomainFactory domainFactory, IDataStore dataStore)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _domainFactory = domainFactory;
        _dataStore = dataStore;
    }

    internal string ContainerName => typeof(TReadModelEntity).GetEntityNameSafe();

    public Guid InstanceId { get; }

    public async Task<Result<TReadModelEntity, Error>> CreateAsync(string id, Action<TReadModelEntity>? action = null,
        CancellationToken cancellationToken = default)
    {
        if (id.IsNotValuedParameter(nameof(id), Resources.ReadModelStore_NoId, out var error))
        {
            return error;
        }

        var entity = new TReadModelEntity { Id = id };
        action?.Invoke(entity);

        var added = await _dataStore.AddAsync(ContainerName, CommandEntity.FromType(entity), cancellationToken);
        if (added.IsFailure)
        {
            return added.Error;
        }

        _recorder.TraceDebug(null, "Created new readmodel entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);
        return added.Value.ToReadModelDto<TReadModelEntity>(_domainFactory);
    }

    public async Task<Result<Error>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (id.IsNotValuedParameter(nameof(id), Resources.ReadModelStore_NoId, out var error))
        {
            return error;
        }

        var retrieved = await _dataStore.RetrieveAsync(ContainerName, id,
            PersistedEntityMetadata.FromType<TReadModelEntity>(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var removed = await _dataStore.RemoveAsync(ContainerName, id, cancellationToken);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        _recorder.TraceDebug(null, "Deleted readmodel entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);

        return Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _dataStore.DestroyAllAsync(ContainerName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All readmodel entities were deleted from the {Store} store",
                _dataStore.GetType().Name);
        }

        return deleted;
    }
#endif

    public async Task<Result<Optional<TReadModelEntity>, Error>> GetAsync(string id, bool errorIfNotFound = true,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var retrieved = await _dataStore.RetrieveAsync(ContainerName, id,
            PersistedEntityMetadata.FromType<TReadModelEntity>(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TReadModelEntity>.None;
        }

        var entity = retrieved.Value.Value;
        if (entity.IsDeleted.ValueOrDefault && !includeDeleted)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TReadModelEntity>.None;
        }

        _recorder.TraceDebug(null, "Readmodel entity {Id} was retrieved from the {Store} store", id,
            _dataStore.GetType().Name);
        return new Result<Optional<TReadModelEntity>, Error>(entity.ToReadModelDto<TReadModelEntity>(_domainFactory));
    }

    public async Task<Result<QueryResults<TReadModelEntity>, Error>> QueryAsync(QueryClause<TReadModelEntity> query,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (query.Options.IsEmpty)
        {
            _recorder.TraceDebug(null, "No readmodel entities were retrieved from the store, the query is empty");

            return new QueryResults<TReadModelEntity>(new List<TReadModelEntity>());
        }

        var queryResults = await _dataStore.QueryAsync(ContainerName, query,
            PersistedEntityMetadata.FromType<TReadModelEntity>(),
            cancellationToken);
        if (queryResults.IsFailure)
        {
            return queryResults.Error;
        }

        var entities = queryResults.Value;

        entities = entities
            .Where(e => !e.IsDeleted.ValueOrDefault || includeDeleted)
            .ToList();

        _recorder.TraceDebug(null, "{Count} readmodel entities were retrieved from the {Store} store", entities.Count,
            _dataStore.GetType().Name);
        return new QueryResults<TReadModelEntity>(entities.ConvertAll(x => x.ToDto<TReadModelEntity>()));
    }

    public async Task<Result<TReadModelEntity, Error>> UpdateAsync(string id, Action<TReadModelEntity> action,
        CancellationToken cancellationToken)
    {
        if (id.IsNotValuedParameter(nameof(id), Resources.ReadModelStore_NoId, out var error))
        {
            return error;
        }

        var retrieved = await _dataStore.RetrieveAsync(ContainerName, id,
            PersistedEntityMetadata.FromType<TReadModelEntity>(),
            cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var dto = retrieved.Value.Value.ToReadModelDto<TReadModelEntity>(_domainFactory);
        action(dto);

        var replaced = await _dataStore.ReplaceAsync(ContainerName, id, CommandEntity.FromType(dto), cancellationToken);
        if (replaced.IsFailure)
        {
            return replaced.Error;
        }

        _recorder.TraceDebug(null, "Updated readmodel entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);

        return replaced.Value.Value.ToReadModelDto<TReadModelEntity>(_domainFactory);
    }

    public async Task<Result<TReadModelEntity, Error>> UpsertAsync(TReadModelEntity entity, bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (!entity.Id.HasValue)
        {
            return Error.EntityNotFound(Resources.ReadModelStore_MissingIdentifier);
        }

        var entityId = entity.Id.Value;
        var retrieved = await _dataStore.RetrieveAsync(ContainerName, entityId,
            PersistedEntityMetadata.FromType<TReadModelEntity>(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            var added = await _dataStore.AddAsync(ContainerName, CommandEntity.FromDto(entity), cancellationToken);
            if (added.IsFailure)
            {
                return added.Error;
            }

            _recorder.TraceDebug(null, "Readmodel entity {Id} was added to the {Store} store", added.Value.Id,
                _dataStore.GetType().Name);
            return added.Value.ToReadModelDto<TReadModelEntity>(_domainFactory);
        }

        var found = retrieved.Value.Value;
        if (found.IsDeleted.ValueOrDefault
            && !includeDeleted)
        {
            return Error.EntityNotFound(Resources.ReadModelStore_DtoDeleted);
        }

        var latest = MergeEntities(entity, found);
        if (found.IsDeleted.ValueOrDefault)
        {
            latest.IsDeleted = false;
        }

        var updated = await _dataStore.ReplaceAsync(ContainerName, entityId, latest, cancellationToken);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        _recorder.TraceDebug(null, "Readmodel entity {Id} was updated in the {Store} store", entityId,
            _dataStore.GetType().Name);
        return updated.Value.Value.ToReadModelDto<TReadModelEntity>(_domainFactory);
    }

    private CommandEntity MergeEntities(TReadModelEntity updated, CommandEntity current)
    {
        var currentAsDto = current.ToReadModelDto<TReadModelEntity>(_domainFactory);
        currentAsDto.PopulateWith(updated);
        return CommandEntity.FromDto(currentAsDto);
    }
}