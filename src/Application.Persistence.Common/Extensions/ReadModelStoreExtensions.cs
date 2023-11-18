using Application.Persistence.Interfaces;
using Common;

namespace Application.Persistence.Common.Extensions;

public static class ReadModelStoreExtensions
{
    /// <inheritdoc cref="IReadModelStore{TDto}.CreateAsync" />
    public static async Task<Result<bool, Error>> HandleCreateAsync<TDto>(this IReadModelStore<TDto> readModel,
        string id, Action<TDto> action, CancellationToken cancellationToken)
        where TDto : IReadModelEntity, new()
    {
        return (await readModel.CreateAsync(id, action, cancellationToken))
            .Match<Result<bool, Error>>(_ => true, error => error);
    }

    /// <inheritdoc cref="IReadModelStore{TDto}.DeleteAsync" />
    public static async Task<Result<bool, Error>> HandleDeleteAsync<TDto>(this IReadModelStore<TDto> readModel,
        string id, CancellationToken cancellationToken)
        where TDto : IReadModelEntity, new()
    {
        return (await readModel.DeleteAsync(id, cancellationToken))
            .Match<Result<bool, Error>>(() => true, error => error);
    }

    /// <inheritdoc cref="IReadModelStore{TDto}.UpdateAsync" />
    public static async Task<Result<bool, Error>> HandleUpdateAsync<TDto>(this IReadModelStore<TDto> readModel,
        string id, Action<TDto> action, CancellationToken cancellationToken)
        where TDto : IReadModelEntity, new()
    {
        return (await readModel.UpdateAsync(id, action, cancellationToken))
            .Match<Result<bool, Error>>(_ => true, error => error);
    }
}