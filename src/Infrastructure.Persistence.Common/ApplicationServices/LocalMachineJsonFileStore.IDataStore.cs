#if TESTINGONLY
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Defines a file repository on the local machine, that stores each entity as raw JSON.
///     store is located in named folders under the <see cref="_rootPath" />
/// </summary>
public partial class LocalMachineJsonFileStore : IDataStore
{
    public const string NullToken = @"null";
    private const string DocumentStoreContainerName = "Documents";

    public int MaxQueryResults => 1000;

    public Task<Result<CommandEntity, Error>> AddAsync(string containerName, CommandEntity entity,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        ArgumentNullException.ThrowIfNull(entity);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));

        container.Write(entity.Id, entity.ToFileProperties());

        return Task.FromResult<Result<CommandEntity, Error>>(CommandEntity.FromCommandEntity(
            container.Read(entity.Id).FromFileProperties(entity.Metadata),
            entity));
    }

    public Task<Result<long, Error>> CountAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        containerName.ThrowIfNotValuedParameter(nameof(containerName));

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));

        return Task.FromResult<Result<long, Error>>(container.Count);
    }

    public Task<Result<List<QueryEntity>, Error>> QueryAsync<TQueryableEntity>(string containerName,
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
            return Task.FromResult<Result<List<QueryEntity>, Error>>(new List<QueryEntity>());
        }

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));
        if (container.IsEmpty())
        {
            return Task.FromResult<Result<List<QueryEntity>, Error>>(new List<QueryEntity>());
        }

        var results = query.FetchAllIntoMemory(MaxQueryResults, metadata,
            () => QueryPrimaryEntities(container, metadata),
            entity => QueryJoiningContainer(EnsureContainer(GetDocumentStoreContainerPath(entity.EntityName)),
                entity));

        return Task.FromResult<Result<List<QueryEntity>, Error>>(results);
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

    public Task<Result<Optional<CommandEntity>, Error>> ReplaceAsync(string containerName, string id,
        CommandEntity entity, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(entity);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));

        var entityProperties = entity.ToFileProperties();
        container.Overwrite(id, entityProperties);

        return Task.FromResult<Result<Optional<CommandEntity>, Error>>(CommandEntity
            .FromCommandEntity(entityProperties.FromFileProperties(entity.Metadata), entity).ToOptional());
    }

    public Task<Result<Optional<CommandEntity>, Error>> RetrieveAsync(string containerName, string id,
        PersistedEntityMetadata metadata, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(metadata);

        var container = EnsureContainer(GetDocumentStoreContainerPath(containerName));
        if (container.Exists(id))
        {
            return Task.FromResult<Result<Optional<CommandEntity>, Error>>(CommandEntity.FromCommandEntity(
                container.Read(id).FromFileProperties(metadata), metadata).ToOptional());
        }

        return Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None);
    }

    Task<Result<Error>> IDataStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        var documentStore = EnsureContainer(GetDocumentStoreContainerPath(containerName));
        documentStore.Erase();

        return Task.FromResult(Result.Ok);
    }

    private static string GetDocumentStoreContainerPath(string containerName, string? entityId = null)
    {
        if (entityId.HasValue())
        {
            return $"{DocumentStoreContainerName}/{containerName}/{entityId}";
        }

        return $"{DocumentStoreContainerName}/{containerName}";
    }

    private static Dictionary<string, HydrationProperties> QueryJoiningContainer(
        FileContainer container, QueriedEntity joinedEntity)
    {
        if (container.IsEmpty())
        {
            return new Dictionary<string, HydrationProperties>();
        }

        var metadata = PersistedEntityMetadata.FromType(joinedEntity.Join.Right.EntityType);
        return container.GetEntityIds()
            .ToDictionary(id => id, id => GetEntityFromFile(container, id, metadata));
    }
}

#endif