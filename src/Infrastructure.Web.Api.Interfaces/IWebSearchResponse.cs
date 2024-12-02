using Application.Interfaces;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a response from a SEARCH API
/// </summary>
public interface IWebSearchResponse : IWebResponse
{
    SearchResultMetadata Metadata { get; set; }
}