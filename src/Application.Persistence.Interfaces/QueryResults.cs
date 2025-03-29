using QueryAny;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a set of results from a query
/// </summary>
public class QueryResults<TDto>
    where TDto : IQueryableEntity
{
    public QueryResults(List<TDto> results, int? totalCount = null)
    {
        Results = results;
        TotalCount = totalCount ?? results.Count;
    }

    public QueryResults()
    {
        Results = [];
        TotalCount = 0;
    }

    /// <summary>
    ///     The search results
    /// </summary>
    public List<TDto> Results { get; }

    /// <summary>
    ///     The total number of results, of which the <see cref="Results" /> are a limited subset
    /// </summary>
    public int TotalCount { get; }
}