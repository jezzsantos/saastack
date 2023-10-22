namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request of a SEARCH API
/// </summary>
public class SearchRequest<TResponse> : IWebSearchRequest<TResponse>
    where TResponse : IWebResponse
{
    public string? Embed { get; set; }

    public string? Filter { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public string? Sort { get; set; }
}