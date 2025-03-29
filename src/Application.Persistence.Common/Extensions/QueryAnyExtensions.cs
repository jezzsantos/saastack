using System.Linq.Expressions;
using System.Reflection;
using Application.Interfaces;
using Common;
using Common.Extensions;
using QueryAny;

namespace Application.Persistence.Common.Extensions;

public static class QueryAnyExtensions
{
    /// <summary>
    ///     Determines whether the specified query is for paginated results.
    /// </summary>
    public static bool IsPaginating<TEntity>(this QueryClause<TEntity> query, int resultsCount)
        where TEntity : IQueryableEntity
    {
        if (query.ResultOptions.Offset > ResultOptions.DefaultOffset)
        {
            return true;
        }

        var hasCustomLimit = query.ResultOptions.Limit > ResultOptions.DefaultLimit;
        if (!hasCustomLimit)
        {
            return false;
        }

        var hasMoreResultsThanLimit = resultsCount >= query.ResultOptions.Limit;
        if (hasMoreResultsThanLimit)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Updates the specified <see cref="query" /> with the specified <see cref="options" />
    /// </summary>
    public static QueryClause<TEntity> WithSearchOptions<TEntity>(this QueryClause<TEntity> query,
        SearchOptions options)
        where TEntity : IQueryableEntity
    {
        if (options.Offset > ResultOptions.DefaultOffset)
        {
            query.Skip(options.Offset);
        }

        if (options.Limit > ResultOptions.DefaultLimit)
        {
            query.Take(options.Limit);
        }

        if (options.Sort.HasValue && options.Sort.Value.By.HasValue())
        {
            var propertyName = options.Sort.Value.By;
            var propertyExpression = GetPropertyExpression<TEntity>(propertyName);
            if (propertyExpression.HasValue)
            {
                query.OrderBy(propertyExpression.Value, options.Sort.Value.Direction == SortDirection.Ascending
                    ? OrderDirection.Ascending
                    : OrderDirection.Descending);
            }
        }

        if (options.Filter.Fields.Any())
        {
            foreach (var field in options.Filter.Fields)
            {
                var propertyExpression = GetPropertyExpression<TEntity>(field);
                if (propertyExpression.HasValue)
                {
                    query.Select(propertyExpression.Value);
                }
            }
        }

        return query;
    }

    private static Optional<Expression<Func<TEntity, object>>> GetPropertyExpression<TEntity>(string propertyName)
        where TEntity : IQueryableEntity
    {
        var propertyInfo = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(info => info.Name.EqualsIgnoreCase(propertyName));
        if (propertyInfo.NotExists())
        {
            return Optional<Expression<Func<TEntity, object>>>.None;
        }

        var variable = Expression.Parameter(typeof(TEntity));
        var propertySelector = Expression.Property(variable, propertyInfo);
        var propertyConversion = Expression.Convert(propertySelector, typeof(object));

        return Expression.Lambda<Func<TEntity, object>>(propertyConversion, variable);
    }
}