using Common;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for writing read model projections
/// </summary>
public interface IReadModelProjectionStore<TReadModelEntity>
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
    ///     Updates an existing read model entity in the store
    /// </summary>
    Task<Result<TReadModelEntity, Error>> UpdateAsync(string id, Action<TReadModelEntity> action,
        CancellationToken cancellationToken);
}