using Common;
using QueryAny;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for reading and writing any read model, that uses snapshotting
/// </summary>
public interface IReadModelStore<TReadModelEntity>
    where TReadModelEntity : IReadModelEntity, new()
{
    /// <summary>
    ///     Creates a new read model entity in the store
    /// </summary>
    Task<Result<TReadModelEntity, Error>> CreateAsync(string id, Action<TReadModelEntity> action,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes a existing read model entity from the store
    /// </summary>
    Task<Result<Error>> DeleteAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    ///     Permanently destroys all entities in the store
    /// </summary>
#if TESTINGONLY
    Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
#endif

    /// <summary>
    ///     Retrieves the existing record from the store.
    ///     If <see cref="errorIfNotFound" /> is true, and the record does not exist, then
    ///     <see cref="ErrorCode.EntityNotFound" /> is returned, else <see cref="Optional{TValue}.None" /> is returned.
    ///     If <see cref="includeDeleted" /> is false and the record exists as soft-deleted then the error
    ///     <see cref="ErrorCode.EntityNotFound" /> or <see cref="Optional{TValue}.None" /> is returned depending on the value
    ///     of <see cref="errorIfNotFound" />
    /// </summary>
    Task<Result<Optional<TReadModelEntity>, Error>> GetAsync(string id, bool errorIfNotFound = true,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the existing records from the store that match the specified <see cref="query" />.
    ///     If <see cref="includeDeleted" /> is true then sof-deleted records are included in the search results
    /// </summary>
    Task<Result<QueryResults<TReadModelEntity>, Error>> QueryAsync(QueryClause<TReadModelEntity> query,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing read model entity in the store
    /// </summary>
    Task<Result<TReadModelEntity, Error>> UpdateAsync(string id, Action<TReadModelEntity> action,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Updates an existing record in the store or inserts a new record into the store, depending on whether it
    ///     exists in the store or not.
    ///     If the existing record exists as soft-deleted, and <see cref="includeDeleted" /> is false, then the error
    ///     <see cref="ErrorCode.EntityNotFound" /> is returned.
    /// </summary>
    Task<Result<TReadModelEntity, Error>> UpsertAsync(TReadModelEntity entity, bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}