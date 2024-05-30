using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access for [CQRS] commands and queries that use snapshotting for any kind of DTO
/// </summary>
public sealed class SnapshottingStore<TDto> : ISnapshottingStore<TDto>
    where TDto : IPersistableDto, new()
{
    private readonly string _containerName;
    private readonly IDataStore _dataStore;
    private readonly IRecorder _recorder;

    public SnapshottingStore(IRecorder recorder, IDataStore dataStore)
    {
        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _dataStore = dataStore;
        _containerName = typeof(TDto).GetEntityNameSafe();
    }

    public Guid InstanceId { get; }

    public async Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken)
    {
        return await _dataStore.CountAsync(_containerName, cancellationToken);
    }

    public async Task<Result<Error>> DeleteAsync(string id, bool destroy = true,
        CancellationToken cancellationToken = default)
    {
        var retrieved = await _dataStore.RetrieveAsync(_containerName, id, PersistedEntityMetadata.FromType<TDto>(),
            cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Result.Ok;
        }

        if (destroy)
        {
            var removed = await _dataStore.RemoveAsync(_containerName, id, cancellationToken);
            if (removed.IsFailure)
            {
                return removed.Error;
            }

            _recorder.TraceDebug(null, "Entity {Id} was destroyed in the {Store} store", id, _dataStore.GetType().Name);
            return Result.Ok;
        }

        var entity = retrieved.Value.Value;
        if (entity.IsDeleted.ValueOrDefault)
        {
            return Result.Ok;
        }

        entity.IsDeleted = true;
        await _dataStore.ReplaceAsync(_containerName, id, entity, cancellationToken);
        _recorder.TraceDebug(null, "Entity {Id} was soft-deleted in the {Store} store", id, _dataStore.GetType().Name);

        return Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _dataStore.DestroyAllAsync(_containerName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All entities were deleted from the {Store} store", _dataStore.GetType().Name);
        }

        return deleted;
    }
#endif

    public async Task<Result<Optional<TDto>, Error>> GetAsync(string id, bool errorIfNotFound = true,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var retrieved = await _dataStore.RetrieveAsync(_containerName, id,
            PersistedEntityMetadata.FromType<TDto>(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TDto>.None;
        }

        var entity = retrieved.Value.Value;
        if (entity.IsDeleted.ValueOrDefault && !includeDeleted)
        {
            return errorIfNotFound
                ? Error.EntityNotFound()
                : Optional<TDto>.None;
        }

        _recorder.TraceDebug(null, "Entity {Id} was retrieved from the {Store} store", id, _dataStore.GetType().Name);
        return new Result<Optional<TDto>, Error>(entity.ToDto<TDto>());
    }

    public async Task<Result<QueryResults<TDto>, Error>> QueryAsync(QueryClause<TDto> query,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (query.Options.IsEmpty)
        {
            _recorder.TraceDebug(null, "No entities were retrieved from the store, the query is empty");

            return new QueryResults<TDto>(new List<TDto>());
        }

        var queryResults = await _dataStore.QueryAsync(_containerName, query, PersistedEntityMetadata.FromType<TDto>(),
            cancellationToken);
        if (queryResults.IsFailure)
        {
            return queryResults.Error;
        }

        var entities = queryResults.Value;

        entities = entities
            .Where(e => !e.IsDeleted.ValueOrDefault || includeDeleted)
            .ToList();

        _recorder.TraceDebug(null, "{Count} entities were retrieved from the {Store} store", entities.Count,
            _dataStore.GetType().Name);
        return new QueryResults<TDto>(entities.ConvertAll(x => x.ToDto<TDto>()));
    }

    public async Task<Result<TDto, Error>> ResurrectDeletedAsync(string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _dataStore.RetrieveAsync(_containerName, id, PersistedEntityMetadata.FromType<TDto>(),
            cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var dto = retrieved.Value.Value;
        if (!dto.IsDeleted.ValueOrDefault)
        {
            return dto.ToDto<TDto>();
        }

        dto.IsDeleted = false;
        var replaced = await _dataStore.ReplaceAsync(_containerName, id, dto, cancellationToken);
        if (replaced.IsFailure)
        {
            return replaced.Error;
        }

        _recorder.TraceDebug(null, "Entity {Id} was resurrected in the {Store} store", id, _dataStore.GetType().Name);
        return dto.ToDto<TDto>();
    }

    public async Task<Result<TDto, Error>> UpsertAsync(TDto dto, bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (!dto.Id.HasValue)
        {
            return Error.EntityNotFound(Resources.SnapshottingStore_DtoMissingIdentifier);
        }

        var dtoId = dto.Id.Value;
        var retrieved = await _dataStore.RetrieveAsync(_containerName, dtoId,
            PersistedEntityMetadata.FromType<TDto>(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            var added = await _dataStore.AddAsync(_containerName, CommandEntity.FromDto(dto), cancellationToken);
            if (added.IsFailure)
            {
                return added.Error;
            }

            _recorder.TraceDebug(null, "Entity {Id} was added to the {Store} store", added.Value.Id,
                _dataStore.GetType().Name);
            return added.Value.ToDto<TDto>();
        }

        var found = retrieved.Value.Value;
        if (found.IsDeleted.ValueOrDefault
            && !includeDeleted)
        {
            return Error.EntityNotFound(Resources.SnapshottingStore_DtoDeleted);
        }

        var latest = MergeDtos(dto, found);
        if (found.IsDeleted.ValueOrDefault)
        {
            latest.IsDeleted = false;
        }

        var updated = await _dataStore.ReplaceAsync(_containerName, dtoId, latest, cancellationToken);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        _recorder.TraceDebug(null, "Entity {Id} was updated in the {Store} store", dtoId, _dataStore.GetType().Name);
        return updated.Value.Value.ToDto<TDto>();
    }

    private static CommandEntity MergeDtos(TDto updated, CommandEntity current)
    {
        var currentAsDto = current.ToDto<TDto>();
        currentAsDto.PopulateWith(updated);
        return CommandEntity.FromDto(currentAsDto);
    }
}