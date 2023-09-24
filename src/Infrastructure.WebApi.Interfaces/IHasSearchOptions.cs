namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Options for SEARCH operations
/// </summary>
public interface IHasSearchOptions : IHasGetOptions
{
    /// <summary>
    ///     Fields to include and exclude in the search result
    /// </summary>
    string? Filter { get; set; }

    /// <summary>
    ///     The maximum number of search results to return
    /// </summary>
    int? Limit { get; set; }

    /// <summary>
    ///     The zero-based index of the first search result
    /// </summary>
    int? Offset { get; set; }

    /// <summary>
    ///     Fields to sort the results on
    /// </summary>
    string? Sort { get; set; }
}