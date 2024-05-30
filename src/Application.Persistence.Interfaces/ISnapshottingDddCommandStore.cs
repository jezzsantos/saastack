using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for reading/writing individual DDD Aggregate by [CQRS] commands, that use snapshotting
/// </summary>
public interface ISnapshottingDddCommandStore<TAggregateRootOrEntity> : IEventNotifyingStore
    where TAggregateRootOrEntity : IDehydratableEntity
{
    /// <summary>
    ///     Returns the total count of entities in the store
    /// </summary>
    Task<Result<long, Error>> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Permanently destroys (or soft-deletes) the existing entity from the store.
    ///     If the entity does not exist, then no error is returned.
    ///     If <see cref="destroy" /> is true then the entity is permanently deleted from the store.
    ///     If <see cref="destroy" /> is false then the entity is updated as soft-deleted in the store.
    /// </summary>
    Task<Result<Error>> DeleteAsync(Identifier id, bool destroy = true, CancellationToken cancellationToken = default);

#if TESTINGONLY
    /// <summary>
    ///     Permanently destroys all entities in the store
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
#endif

    /// <summary>
    ///     Retrieves the existing entity from the store.
    ///     If <see cref="errorIfNotFound" /> is true, and the entity does not exist, then
    ///     <see cref="ErrorCode.EntityNotFound" /> is returned, else <see cref="Optional{TValue}.None" /> is returned.
    ///     If <see cref="includeDeleted" /> is false and the entity exists as soft-deleted then the error
    ///     <see cref="ErrorCode.EntityNotFound" /> or <see cref="Optional{TValue}.None" /> is returned depending on the value
    ///     of <see cref="errorIfNotFound" />
    /// </summary>
    Task<Result<Optional<TAggregateRootOrEntity>, Error>> GetAsync(Identifier id, bool errorIfNotFound = true,
        bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resurrects the soft-deleted entity from the store.
    ///     If the entity does not exist, then <see cref="ErrorCode.EntityNotFound" /> is returned
    ///     If the entity exists and is not soft-deleted then it is returned as is.
    /// </summary>
    Task<Result<TAggregateRootOrEntity, Error>> ResurrectDeletedAsync(Identifier id,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Updates an existing entity in the store or inserts a new entity into the store, depending on whether it
    ///     exists in the store or not.
    ///     If the existing entity exists as soft-deleted, and <see cref="includeDeleted" /> is false, then the error
    ///     <see cref="ErrorCode.EntityNotFound" /> is returned.
    /// </summary>
    Task<Result<TAggregateRootOrEntity, Error>> UpsertAsync(TAggregateRootOrEntity entity, bool includeDeleted = false,
        CancellationToken cancellationToken = default);
}