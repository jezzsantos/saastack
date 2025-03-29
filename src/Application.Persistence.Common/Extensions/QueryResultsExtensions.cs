using Application.Interfaces;
using Application.Interfaces.Extensions;
using Application.Interfaces.Resources;
using Application.Persistence.Interfaces;
using QueryAny;

namespace Application.Persistence.Common.Extensions;

public static class QueryResultsExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="List{TResource}" /> to a <see cref="SearchResults{TResource}" /> object,
    ///     considering the specified <see cref="SearchOptions" />.
    /// </summary>
    public static SearchResults<TResource> ToSearchResults<TResource>(this List<TResource> results,
        SearchOptions searchOptions)
        where TResource : IIdentifiableResource
    {
        return new SearchResults<TResource>
        {
            Results = results,
            Metadata = searchOptions.ToMetadata(results.Count)
        };
    }

    /// <summary>
    ///     Converts the specified <see cref="QueryResults{TEntity}" /> to a <see cref="SearchResults{TEntity}" /> object,
    ///     considering the specified <see cref="SearchOptions" />.
    /// </summary>
    public static SearchResults<TEntity> ToSearchResults<TEntity>(this QueryResults<TEntity> results,
        SearchOptions searchOptions)
        where TEntity : IQueryableEntity
    {
        return new SearchResults<TEntity>
        {
            Results = results.Results,
            Metadata = searchOptions.ToMetadata(results.TotalCount)
        };
    }

    /// <summary>
    ///     Converts the specified <see cref="QueryResults{TReadModel}" /> to a <see cref="SearchResults{TResource}" /> object,
    ///     considering the specified <see cref="SearchOptions" />.
    /// </summary>
    public static SearchResults<TResource> ToSearchResults<TResource, TReadModel>(this QueryResults<TReadModel> results,
        SearchOptions searchOptions, Func<TReadModel, TResource> converter)
        where TResource : IIdentifiableResource
        where TReadModel : IQueryableEntity
    {
        var resources = results.Results
            .Select(converter)
            .ToList();

        return new SearchResults<TResource>
        {
            Results = resources,
            Metadata = searchOptions.ToMetadata(results.TotalCount)
        };
    }
}