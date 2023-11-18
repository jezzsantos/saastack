using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

public class ReadModelStore<TDto> : IReadModelStore<TDto>
    where TDto : IReadModelEntity, new()
{
    private readonly IDataStore _dataStore;
    private readonly IDomainFactory _domainFactory;
    private readonly IRecorder _recorder;

    public ReadModelStore(IRecorder recorder, IDomainFactory domainFactory, IDataStore dataStore)
    {
        _recorder = recorder;
        _domainFactory = domainFactory;
        _dataStore = dataStore;
    }

    internal string ContainerName => typeof(TDto).GetEntityNameSafe();

    public async Task<Result<TDto, Error>> CreateAsync(string id, Action<TDto>? action = null,
        CancellationToken cancellationToken = default)
    {
        if (id.IsNotValuedParameter(nameof(id), Resources.ReadModelStore_NoId, out var error))
        {
            return error;
        }

        var dto = new TDto { Id = id };
        action?.Invoke(dto);

        var added = await _dataStore.AddAsync(ContainerName, CommandEntity.FromType(dto), cancellationToken);
        if (!added.IsSuccessful)
        {
            return added.Error;
        }

        var entity = added.Value;
        _recorder.TraceDebug(null, "Created new readmodel for entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);
        return entity.ToReadModelDto<TDto>(_domainFactory);
    }

    public async Task<Result<Error>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (id.IsNotValuedParameter(nameof(id), Resources.ReadModelStore_NoId, out var error))
        {
            return error;
        }

        var retrieved = await _dataStore.RetrieveAsync(ContainerName, id,
            PersistedEntityMetadata.FromType<TDto>(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var removed = await _dataStore.RemoveAsync(ContainerName, id, cancellationToken);
        if (!removed.IsSuccessful)
        {
            return removed.Error;
        }

        _recorder.TraceDebug(null, "Deleted readmodel for entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);

        return Result.Ok;
    }

    public async Task<Result<TDto, Error>> UpdateAsync(string id, Action<TDto> action,
        CancellationToken cancellationToken)
    {
        if (id.IsNotValuedParameter(nameof(id), Resources.ReadModelStore_NoId, out var error))
        {
            return error;
        }

        var retrieved = await _dataStore.RetrieveAsync(ContainerName, id, PersistedEntityMetadata.FromType<TDto>(),
            cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var dto = retrieved.Value.Value.ToReadModelDto<TDto>(_domainFactory);
        action(dto);

        var replaced = await _dataStore.ReplaceAsync(ContainerName, id, CommandEntity.FromType(dto), cancellationToken);
        if (!replaced.IsSuccessful)
        {
            return replaced.Error;
        }

        _recorder.TraceDebug(null, "Updated readmodel for entity {Id} in the {Store} store", id,
            _dataStore.GetType().Name);

        return replaced.Value.Value.ToReadModelDto<TDto>(_domainFactory);
    }
}