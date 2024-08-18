#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

partial class InProcessInMemStore : IDataStore
{
    private readonly Dictionary<string, Dictionary<string, HydrationProperties>> _documents = new();

    public Task<Result<CommandEntity, Error>> AddAsync(string containerName, CommandEntity entity,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        ArgumentNullException.ThrowIfNull(entity);

        if (!_documents.TryGetValue(containerName, out var document))
        {
            document = new Dictionary<string, HydrationProperties>();
            _documents.Add(containerName, document);
        }

        document.Add(entity.Id, entity.ToHydrationProperties());

        return Task.FromResult<Result<CommandEntity, Error>>(
            CommandEntity.FromCommandEntity(document[entity.Id], entity));
    }

    public Task<Result<long, Error>> CountAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        return Task.FromResult<Result<long, Error>>(_documents.TryGetValue(containerName, out var value)
            ? value.Count
            : 0);
    }

#if TESTINGONLY
    Task<Result<Error>> IDataStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        _documents.Remove(containerName);

        return Task.FromResult(Result.Ok);
    }
#endif

    public int MaxQueryResults => 1000;

    public async Task<Result<List<QueryEntity>, Error>> QueryAsync<TQueryableEntity>(string containerName,
        QueryClause<TQueryableEntity> query, PersistedEntityMetadata metadata,
        CancellationToken cancellationToken)
        where TQueryableEntity : IQueryableEntity
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(metadata);

        if (query.NotExists() || query.Options.IsEmpty)
        {
            return new List<QueryEntity>();
        }

        if (!_documents.ContainsKey(containerName))
        {
            return new List<QueryEntity>();
        }

        var results = await query.FetchAllIntoMemoryAsync(MaxQueryResults, metadata,
            () => QueryPrimaryEntitiesAsync(containerName, cancellationToken),
            entity => QueryJoiningContainerAsync(entity, cancellationToken));

        return results;
    }

    public Task<Result<Error>> RemoveAsync(string containerName, string id, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);

        if (_documents.TryGetValue(containerName, out var document)
            && document.ContainsKey(id))
        {
            document.Remove(id);
        }

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Optional<CommandEntity>, Error>> ReplaceAsync(string containerName, string id,
        CommandEntity entity, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(entity);

        var entityProperties = entity.ToHydrationProperties();
        _documents[containerName][id] = entityProperties;

        return Task.FromResult<Result<Optional<CommandEntity>, Error>>(CommandEntity
            .FromCommandEntity(entityProperties, entity).ToOptional());
    }

    public Task<Result<Optional<CommandEntity>, Error>> RetrieveAsync(string containerName, string id,
        PersistedEntityMetadata metadata, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(metadata);

        if (_documents.TryGetValue(containerName, out var document)
            && document.TryGetValue(id, out var properties))
        {
            return Task.FromResult<Result<Optional<CommandEntity>, Error>>(CommandEntity
                .FromCommandEntity(properties, metadata).ToOptional());
        }

        return Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None);
    }

    private Task<Dictionary<string, HydrationProperties>> QueryPrimaryEntitiesAsync(string containerName,
        CancellationToken _)
    {
        return Task.FromResult(_documents[containerName]);
    }

    private Task<Dictionary<string, HydrationProperties>> QueryJoiningContainerAsync(
        QueriedEntity joinedEntity, CancellationToken _)
    {
        return Task.FromResult(_documents.TryGetValue(joinedEntity.EntityName, out var value)
            ? value.ToDictionary(pair => pair.Key, pair => pair.Value)
            : new Dictionary<string, HydrationProperties>());
    }
}
#endif