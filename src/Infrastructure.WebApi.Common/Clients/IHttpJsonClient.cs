using Infrastructure.WebApi.Interfaces;

namespace Infrastructure.WebApi.Common.Clients;

/// <summary>
///     Defines a JSON <see cref="HttpClient" />
/// </summary>
public interface IHttpJsonClient
{
    Task<JsonResponse> DeleteAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new();

    Task<JsonResponse> DeleteAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default);

    Task<JsonResponse<TResponse>> GetAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new();

    Task<JsonResponse> GetAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default);

    Task<JsonResponse<TResponse>> PatchAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new();

    Task<JsonResponse> PatchAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default);

    Task<JsonResponse<TResponse>> PostAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new();

    Task<JsonResponse> PostAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default);

    Task<JsonResponse<TResponse>> PutAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new();

    Task<JsonResponse> PutAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default);
}