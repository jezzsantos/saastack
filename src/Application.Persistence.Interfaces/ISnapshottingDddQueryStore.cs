using Common;
using Domain.Common.ValueObjects;
using QueryAny;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for reading individual/collections of DDD Aggregate by [CQRS] queries, that use snapshotting
/// </summary>
public interface ISnapshottingDddQueryStore<TEntity>
    where TEntity : IQueryableEntity, new()
{
    /// <summary>
    ///     Returns the total count of entities in the store
    /// </summary>
    Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Permanently destroys all entities in the store
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves the existing entity from the store.
    ///     If <see cref="errorIfNotFound" /> is true, and the entity does not exist, then
    ///     <see cref="ErrorCode.EntityNotFound" /> is returned, else <see cref="Optional{TValue}.None" /> is returned.
    ///     If <see cref="includeDeleted" /> is false and the entity exists as soft-deleted then the error
    ///     <see cref="ErrorCode.EntityNotFound" /> or <see cref="Optional{TValue}.None" /> is returned depending on the value
    ///     of <see cref="errorIfNotFound" />
    /// </summary>
    Task<Result<Optional<TEntityWithId>, Error>> GetAsync<TEntityWithId>(Identifier id, bool errorIfNotFound = true,
        bool includeDeleted = false, CancellationToken cancellationToken = default)
        where TEntityWithId : IQueryableEntity, IHasIdentity, new();

    /// <summary>
    ///     Retrieves the existing entities from the store that match the specified <see cref="query" />.
    ///     If <see cref="includeDeleted" /> is true then sof-deleted entities are included in the search results
    /// </summary>
    Task<Result<QueryResults<TEntity>, Error>> QueryAsync(QueryClause<TEntity> query, bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}