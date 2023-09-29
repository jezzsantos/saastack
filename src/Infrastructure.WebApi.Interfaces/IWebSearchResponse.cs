using Application.Interfaces;

namespace Infrastructure.WebApi.Interfaces;

public interface IWebSearchResponse : IWebResponse
{
    SearchResultMetadata? Metadata { get; set; }
}