using Common;
using QueryAny;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines read/write access to individual/collections of data entities to and from a data store
///     (e.g. a database (relational or not), cache, or any cloud data repository)
/// </summary>
public interface IDataStore
{
    int MaxQueryResults { get; }

    Task<Result<CommandEntity, Error>> AddAsync(string containerName, CommandEntity entity,
        CancellationToken cancellationToken);

    Task<Result<long, Error>> CountAsync(string containerName, CancellationToken cancellationToken);

    Task<Result<Error>> DestroyAllAsync(string containerName, CancellationToken cancellationToken);

    Task<Result<List<QueryEntity>, Error>> QueryAsync<TQueryableEntity>(string containerName,
        QueryClause<TQueryableEntity> query, PersistedEntityMetadata metadata, CancellationToken cancellationToken)
        where TQueryableEntity : IQueryableEntity;

    Task<Result<Error>> RemoveAsync(string containerName, string id, CancellationToken cancellationToken);

    Task<Result<Optional<CommandEntity>, Error>> ReplaceAsync(string containerName, string id, CommandEntity entity,
        CancellationToken cancellationToken);

    Task<Result<Optional<CommandEntity>, Error>> RetrieveAsync(string containerName, string id,
        PersistedEntityMetadata metadata, CancellationToken cancellationToken);
}