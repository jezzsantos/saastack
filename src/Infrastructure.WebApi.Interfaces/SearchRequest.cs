namespace Infrastructure.WebApi.Interfaces;

public class SearchRequest<TResponse> : IWebSearchRequest<TResponse>
    where TResponse : IWebResponse
{
    public string? Embed { get; set; }

    public string? Filter { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public string? Sort { get; set; }
}