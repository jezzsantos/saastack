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

        var hydrationProperties = document[entity.Id];
        ApplyMappings(entity.Metadata, hydrationProperties);

        return Task.FromResult<Result<CommandEntity, Error>>(
            CommandEntity.FromHydrationProperties(hydrationProperties, entity));
    }

    public Task<Result<long, Error>> CountAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        return Task.FromResult<Result<long, Error>>(_documents.TryGetValue(containerName, out var value)
            ? value.Count
            : 0);
    }

    Task<Result<Error>> IDataStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        _documents.Remove(containerName);

        return Task.FromResult(Result.Ok);
    }

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
            () => QueryPrimaryEntitiesAsync(containerName, metadata, cancellationToken),
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

        var hydrationProperties = entity.ToHydrationProperties();
        _documents[containerName][id] = hydrationProperties;
        ApplyMappings(entity.Metadata, hydrationProperties);

        return Task.FromResult<Result<Optional<CommandEntity>, Error>>(CommandEntity
            .FromHydrationProperties(hydrationProperties, entity).ToOptional());
    }

    public Task<Result<Optional<CommandEntity>, Error>> RetrieveAsync(string containerName, string id,
        PersistedEntityMetadata metadata, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(metadata);

        if (_documents.TryGetValue(containerName, out var document)
            && document.TryGetValue(id, out var hydrationProperties))
        {
            ApplyMappings(metadata, hydrationProperties);
            return Task.FromResult<Result<Optional<CommandEntity>, Error>>(CommandEntity
                .FromHydrationProperties(hydrationProperties, metadata).ToOptional());
        }

        return Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None);
    }

    private Task<Dictionary<string, HydrationProperties>> QueryPrimaryEntitiesAsync(string containerName,
        PersistedEntityMetadata metadata,
        CancellationToken _)
    {
        var documents = _documents[containerName];
        foreach (var document in documents)
        {
            ApplyMappings(metadata, document.Value);
        }

        return Task.FromResult(documents);
    }

    private Task<Dictionary<string, HydrationProperties>> QueryJoiningContainerAsync(
        QueriedEntity joinedEntity, CancellationToken _)
    {
        var metadata = PersistedEntityMetadata.FromType(joinedEntity.Join.Right.EntityType);

        var documents = _documents.TryGetValue(joinedEntity.EntityName, out var value)
            ? value.ToDictionary(pair => pair.Key, pair => pair.Value)
            : new Dictionary<string, HydrationProperties>();
        foreach (var document in documents)
        {
            ApplyMappings(metadata, document.Value);
        }

        return Task.FromResult(documents);
    }

    private static void ApplyMappings(PersistedEntityMetadata metadata,
        HydrationProperties containerProperties)
    {
        if (containerProperties.HasNone())
        {
            return;
        }

        var mappings = metadata.GetReadMappingsOverride();
        if (mappings.HasAny())
        {
            var containerPropertiesDictionary = containerProperties
                .ToDictionary(pair => pair.Key, pair => pair.Value.ValueOrNull);
            foreach (var mapping in mappings)
            {
                var mapResult = Try.Safely(() => mapping.Value(containerPropertiesDictionary));
                if (mapResult.Exists())
                {
                    containerProperties.AddOrUpdate(mapping.Key, mapResult.ToOptional());
                }
            }
        }
    }
}
#endif