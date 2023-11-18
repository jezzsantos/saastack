using QueryAny;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a set of results from a query
/// </summary>
public class QueryResults<TDto>
    where TDto : IQueryableEntity
{
    public QueryResults(List<TDto> results)
    {
        Results = results;
    }

    public List<TDto> Results { get; }
}