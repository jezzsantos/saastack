using System.Net;
using System.Net.Http.Headers;
using Infrastructure.WebApi.Interfaces;

namespace Infrastructure.WebApi.Common.Clients;

/// <summary>
///     Defines a JSON <see cref="HttpClient" />
/// </summary>
public interface IHttpJsonClient
{
    Task<JsonResponseMessage> DeleteAsync(string requestUri, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null);

    Task<JsonResponseMessage<TResponse>> DeleteAsync<TResponse>(string requestUri,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new();

    Task<JsonResponseMessage> GetAsync(string requestUri, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null);

    Task<JsonResponseMessage<TResponse>> GetAsync<TResponse>(string requestUri,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new();

    Task<JsonResponseMessage<TResponse>> PatchAsync<TResponse>(string requestUri, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new();

    Task<JsonResponseMessage> PatchAsync(string requestUri, StringContent request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null);

    Task<JsonResponseMessage<TResponse>> PostAsync<TResponse>(string requestUri, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new();

    Task<JsonResponseMessage> PostAsync(string requestUri, StringContent request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null);

    Task<JsonResponseMessage<TResponse>> PutAsync<TResponse>(string requestUri, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new();

    Task<JsonResponseMessage> PutAsync(string requestUri, StringContent request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null);
}

public class JsonResponseMessage
{
    public string? Content { get; init; }

    public required HttpResponseHeaders Headers { get; set; }

    public required string RequestId { get; init; }

    public required HttpStatusCode StatusCode { get; init; }
}

public class JsonResponseMessage<TResponse> : JsonResponseMessage
    where TResponse : IWebResponse
{
    public new required TResponse Content { get; init; }
}