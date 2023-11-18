using Common;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for writing read model DTOs
/// </summary>
public interface IReadModelStore<TDto>
    where TDto : IReadModelEntity, new()
{
    /// <summary>
    ///     Creates a new read model entity in the store
    /// </summary>
    Task<Result<TDto, Error>> CreateAsync(string id, Action<TDto> action, CancellationToken cancellationToken);

    /// <summary>
    ///     Deletes a existing read model entity from the store
    /// </summary>
    Task<Result<Error>> DeleteAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates an existing read model entity in the store
    /// </summary>
    Task<Result<TDto, Error>> UpdateAsync(string id, Action<TDto> action, CancellationToken cancellationToken);
}