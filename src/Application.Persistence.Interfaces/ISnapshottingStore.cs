using Common;
using QueryAny;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for reading and writing any DTO, that uses snapshotting
/// </summary>
public interface ISnapshottingStore<TDto>
    where TDto : IPersistableDto, new()
{
    /// <summary>
    ///     Returns the total count of records in the store
    /// </summary>
    Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Permanently destroys (or soft-deletes) the existing record from the store.
    ///     If the record does not exist, then no error is returned.
    ///     If <see cref="destroy" /> is true then the record is permanently deleted from the store.
    ///     If <see cref="destroy" /> is false then the record is updated as soft-deleted in the store.
    /// </summary>
    Task<Result<Error>> DeleteAsync(string id, bool destroy = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Permanently destroys all records in the store
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves the existing record from the store.
    ///     If <see cref="errorIfNotFound" /> is true, and the record does not exist, then
    ///     <see cref="ErrorCode.EntityNotFound" /> is returned, else <see cref="Optional{TValue}.None" /> is returned.
    ///     If <see cref="includeDeleted" /> is false and the record exists as soft-deleted then the error
    ///     <see cref="ErrorCode.EntityNotFound" /> or <see cref="Optional{TValue}.None" /> is returned depending on the value
    ///     of <see cref="errorIfNotFound" />
    /// </summary>
    Task<Result<Optional<TDto>, Error>> GetAsync(string id, bool errorIfNotFound = true, bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the existing records from the store that match the specified <see cref="query" />.
    ///     If <see cref="includeDeleted" /> is true then sof-deleted records are included in the search results
    /// </summary>
    Task<Result<QueryResults<TDto>, Error>> QueryAsync(QueryClause<TDto> query, bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resurrects the soft-deleted record from the store.
    ///     If the record does not exist, then <see cref="ErrorCode.EntityNotFound" /> is returned.
    ///     If the record exists and is not soft-deleted then it is returned as is.
    /// </summary>
    Task<Result<TDto, Error>> ResurrectDeletedAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates an existing record in the store or inserts a new record into the store, depending on whether it
    ///     exists in the store or not.
    ///     If the existing record exists as soft-deleted, and <see cref="includeDeleted" /> is false, then the error
    ///     <see cref="ErrorCode.EntityNotFound" /> is returned.
    /// </summary>
    Task<Result<TDto, Error>> UpsertAsync(TDto dto, bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}