using Application.Persistence.Interfaces;
using Common;

namespace Application.Persistence.Common.Extensions;

public static class ReadModelStoreExtensions
{
    /// <inheritdoc cref="IReadModelStore{TReadModelEntity}.CreateAsync" />
    public static async Task<Result<bool, Error>> HandleCreateAsync<TReadModelEntity>(
        this IReadModelStore<TReadModelEntity> store,
        string id, Action<TReadModelEntity> action, CancellationToken cancellationToken)
        where TReadModelEntity : IReadModelEntity, new()
    {
        return (await store.CreateAsync(id, action, cancellationToken))
            .Match<Result<bool, Error>>(_ => true, error => error);
    }

    /// <inheritdoc cref="IReadModelStore{TReadModelEntity}.DeleteAsync" />
    public static async Task<Result<bool, Error>> HandleDeleteAsync<TReadModelEntity>(
        this IReadModelStore<TReadModelEntity> store,
        string id, CancellationToken cancellationToken)
        where TReadModelEntity : IReadModelEntity, new()
    {
        return (await store.DeleteAsync(id, cancellationToken))
            .Match<Result<bool, Error>>(() => true, error => error);
    }

    /// <inheritdoc cref="IReadModelStore{TReadModelEntity}.UpdateAsync" />
    public static async Task<Result<bool, Error>> HandleUpdateAsync<TReadModelEntity>(
        this IReadModelStore<TReadModelEntity> store,
        string id, Action<TReadModelEntity> action, CancellationToken cancellationToken)
        where TReadModelEntity : IReadModelEntity, new()
    {
        return (await store.UpdateAsync(id, action, cancellationToken))
            .Match<Result<bool, Error>>(_ => true, error => error);
    }
}