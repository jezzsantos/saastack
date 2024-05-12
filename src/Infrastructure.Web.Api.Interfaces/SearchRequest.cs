using System.ComponentModel;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request of a SEARCH API
/// </summary>
public class SearchRequest<TResponse> : IWebSearchRequest<TResponse>
    where TResponse : IWebSearchResponse
{
    [Description("List of child resources to embed in the resource")]
    public string? Embed { get; set; }

    [Description("List of fields to include and exclude in the search result")]
    public string? Filter { get; set; }

    [Description("The maximum number of search results to return")]
    public int? Limit { get; set; }

    [Description("The zero-based index of the first search result")]
    public int? Offset { get; set; }

    [Description("List of fields to sort the results on")]
    public string? Sort { get; set; }
}