#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

partial class LocalMachineJsonFileStore : IDataStore
{
    public const string NullToken = @"null";
    private const string DocumentStoreContainerName = "Documents";

    public async Task<Result<CommandEntity, Error>> AddAsync(string containerName, CommandEntity entity,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        ArgumentNullException.ThrowIfNull(entity);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));

        await container.WriteAsync(entity.Id, entity.ToFileProperties(), cancellationToken);

        var properties = await container.ReadAsync(entity.Id, cancellationToken);
        return CommandEntity.FromCommandEntity(properties.FromFileProperties(entity.Metadata), entity);
    }

    public Task<Result<long, Error>> CountAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));

        return Task.FromResult<Result<long, Error>>(container.Count);
    }

#if TESTINGONLY
    Task<Result<Error>> IDataStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        var documentStore = EnsureContainer(GetDocumentStoreContainerPath(containerName));
        documentStore.Erase();

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

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));
        if (container.IsEmpty())
        {
            return new List<QueryEntity>();
        }

        var results = await query.FetchAllIntoMemoryAsync(MaxQueryResults, metadata,
            () => QueryPrimaryEntitiesAsync(container, metadata, cancellationToken),
            entity => QueryJoiningContainerAsync(EnsureContainer(GetDocumentStoreContainerPath(entity.EntityName)),
                entity, cancellationToken));

        return results;
    }

    public Task<Result<Error>> RemoveAsync(string containerName, string id, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));
        if (container.Exists(id))
        {
            container.Remove(id);
        }

        return Task.FromResult(Result.Ok);
    }

    public async Task<Result<Optional<CommandEntity>, Error>> ReplaceAsync(string containerName, string id,
        CommandEntity entity, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(entity);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));

        var entityProperties = entity.ToFileProperties();
        await container.OverwriteAsync(id, entityProperties, cancellationToken);

        return CommandEntity.FromCommandEntity(entityProperties.FromFileProperties(entity.Metadata), entity)
            .ToOptional();
    }

    public async Task<Result<Optional<CommandEntity>, Error>> RetrieveAsync(string containerName, string id,
        PersistedEntityMetadata metadata, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(metadata);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));
        if (container.Exists(id))
        {
            var properties = await container.ReadAsync(id, cancellationToken);
            return CommandEntity.FromCommandEntity(
                properties.FromFileProperties(metadata), metadata).ToOptional();
        }

        return Optional<CommandEntity>.None;
    }

    private static string GetDocumentStoreContainerPath(string containerName, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{DocumentStoreContainerName}/{containerName}/{entityId}";
        }

        return $"{DocumentStoreContainerName}/{containerName}";
    }

    private static async Task<Dictionary<string, HydrationProperties>> QueryJoiningContainerAsync(
        FileContainer container, QueriedEntity joinedEntity, CancellationToken cancellationToken)
    {
        if (container.IsEmpty())
        {
            return new Dictionary<string, HydrationProperties>();
        }

        var metadata = PersistedEntityMetadata.FromType(joinedEntity.Join.Right.EntityType);
        var ids = container.GetEntityIds();
        var joiningProperties = new Dictionary<string, HydrationProperties>();
        foreach (var id in ids)
        {
            var properties = await GetEntityFromFileAsync(container, id, metadata, cancellationToken);
            joiningProperties.Add(id, properties);
        }

        return joiningProperties;
    }
}

#endif