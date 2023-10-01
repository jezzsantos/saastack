using Application.Interfaces;

namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines a response from a SEARCH API
/// </summary>
public class SearchResponse : IWebSearchResponse
{
    public SearchResultMetadata? Metadata { get; set; }
}