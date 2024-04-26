using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides a store for writing read model projections
/// </summary>
public class ReadModelProjectionStore<TReadModelEntity> : IReadModelProjectionStore<TReadModelEntity>
    where TReadModelEntity : IReadModelEntity, new()
{
    private readonly IDataStore _dataStore;
    private readonly IDomainFactory _domainFactory;
    private readonly IRecorder _recorder;

    public ReadModelProjectionStore(IRecorder recorder, IDomainFactory domainFactory, IDataStore dataStore)
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

        var dto = new TReadModelEntity { Id = id };
        action?.Invoke(dto);

        var added = await _dataStore.AddAsync(ContainerName, CommandEntity.FromType(dto), cancellationToken);
        if (added.IsFailure)
        {
            return added.Error;
        }

        var entity = added.Value;
        _recorder.TraceDebug(null, "Created new readmodel for entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);
        return entity.ToReadModelDto<TReadModelEntity>(_domainFactory);
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

        _recorder.TraceDebug(null, "Deleted readmodel for entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);

        return Result.Ok;
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

        _recorder.TraceDebug(null, "Updated readmodel for entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);

        return replaced.Value.Value.ToReadModelDto<TReadModelEntity>(_domainFactory);
    }
}