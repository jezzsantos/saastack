namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Defines the request of a SEARCH API
/// </summary>
public interface IWebSearchRequest<TResponse> : IWebRequest<TResponse>, IHasSearchOptions
    where TResponse : IWebResponse
{
}