using System.Linq.Expressions;
using Application.Persistence.Interfaces;
using Common;
using QueryAny;

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

    /// <summary>
    ///     Deletes all the records of the <see cref="TReadModelEntity" /> where the specified <see cref="parentIdMatch" />
    ///     function
    ///     matches the <see cref="id" />
    /// </summary>
    public static async Task<Result<bool, Error>> HandleDeleteRelatedAsync<TReadModelEntity>(
        this IReadModelStore<TReadModelEntity> store,
        string id, Expression<Func<TReadModelEntity, string>> parentIdMatch, CancellationToken cancellationToken)
        where TReadModelEntity : IReadModelEntity, new()
    {
        var query = Query.From<TReadModelEntity>()
            .Where(parentIdMatch, ConditionOperator.EqualTo, id);
        var queried = await store.QueryAsync(query, true, cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        foreach (var entity in queried.Value.Results)
        {
            var deleted = await store.DeleteAsync(entity.Id, cancellationToken);
            if (deleted.IsFailure)
            {
                return deleted.Error;
            }
        }

        return true;
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