using System.ComponentModel;
using Application.Interfaces;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a response from a SEARCH API
/// </summary>
public class SearchResponse : IWebSearchResponse
{
    [Description("Metadata about the search results")]
    public SearchResultMetadata? Metadata { get; set; }
}