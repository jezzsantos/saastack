using Application.Interfaces;

namespace Infrastructure.WebApi.Interfaces;

public class SearchResponse : IWebSearchResponse
{
    public SearchResultMetadata? Metadata { get; set; }
}