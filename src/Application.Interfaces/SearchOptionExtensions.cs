using Common.Extensions;

namespace Application.Interfaces;

public static class SearchOptionExtensions
{
    /// <summary>
    ///     Converts the <see cref="SearchOptions" /> to <see cref="SearchResultMetadata" />
    /// </summary>
    public static SearchResultMetadata ToMetadata(this SearchOptions? options, int total = 0)
    {
        return Map(options.NotExists()
            ? new SearchOptions()
            : options!, total);

        static SearchResultMetadata Map(SearchOptions options, int total)
        {
            return new SearchResultMetadata
            {
                Total = total,
                Limit = options.Limit,
                Offset = options.Offset,
                Sort = options.Sort.ValueOrDefault,
                Filter = options.Filter
            };
        }
    }
}