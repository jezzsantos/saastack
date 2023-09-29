namespace Infrastructure.WebApi.Interfaces;

public interface IWebSearchRequest<TResponse> : IWebRequest<TResponse>, IHasSearchOptions
    where TResponse : IWebResponse
{
}