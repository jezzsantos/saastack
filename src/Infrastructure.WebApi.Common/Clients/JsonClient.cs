using System.Net.Http.Headers;
using System.Net.Http.Json;
using Common.Extensions;
using Infrastructure.WebApi.Interfaces;

namespace Infrastructure.WebApi.Common.Clients;

/// <summary>
///     Provides a convenient typed <see cref="HttpClient" /> that accepts and returns JSON
/// </summary>
public class JsonClient : IHttpJsonClient
{
    private readonly HttpClient _client;

    public JsonClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<JsonResponseMessage> DeleteAsync(string requestUri,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
    {
        var response = await SendRequestAsync(HttpMethod.Delete, requestUri, null, requestFilter, cancellationToken);
        var content = await GetContentAsync(cancellationToken, response);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage<TResponse>> DeleteAsync<TResponse>(string requestUri,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Delete, requestUri, null, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage> GetAsync(string requestUri, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        var response = await SendRequestAsync(HttpMethod.Get, requestUri, null, requestFilter, cancellationToken);
        var content = await GetContentAsync(cancellationToken, response);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage<TResponse>> GetAsync<TResponse>(string requestUri,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Get, requestUri, null, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage<TResponse>> PatchAsync<TResponse>(string requestUri,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Patch, requestUri, request, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage> PatchAsync(string requestUri, StringContent request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
    {
        var response = await SendRequestAsync(HttpMethod.Patch, requestUri, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(cancellationToken, response);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage<TResponse>> PostAsync<TResponse>(string requestUri,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Post, requestUri, request, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage> PostAsync(string requestUri, StringContent request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
    {
        var response = await SendRequestAsync(HttpMethod.Post, requestUri, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(cancellationToken, response);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage<TResponse>> PutAsync<TResponse>(string requestUri,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Put, requestUri, request, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponseMessage> PutAsync(string requestUri, StringContent request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
    {
        var response = await SendRequestAsync(HttpMethod.Put, requestUri, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(cancellationToken, response);

        return CreateResponse(response, content);
    }

    private async Task<HttpResponseMessage> SendRequestAsync<TResponse>(HttpMethod method, string requestUri,
        IWebRequest<TResponse>? requestContent, Action<HttpRequestMessage>? requestFilter,
        CancellationToken? cancellationToken)
        where TResponse : IWebResponse, new()
    {
        var thing = requestContent ?? new object();
        var content = new StringContent(thing.ToJson()!, new MediaTypeHeaderValue(HttpContentTypes.Json));
        return await SendRequestAsync(method, requestUri, content, requestFilter, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string requestUri,
        StringContent? requestContent, Action<HttpRequestMessage>? requestFilter, CancellationToken? cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(requestUri, UriKind.Relative),
            Content = requestContent
        };
        if (requestFilter is not null)
        {
            requestFilter(request);
        }

        return await _client.SendAsync(request, cancellationToken ?? CancellationToken.None);
    }

    private static async Task<TResponse?> GetContentAsync<TResponse>(HttpResponseMessage response,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        return await response.Content.ReadFromJsonAsync<TResponse>(
            cancellationToken: cancellationToken ?? CancellationToken.None) ?? new TResponse();
    }

    private static async Task<string?> GetContentAsync(CancellationToken? cancellationToken,
        HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync(cancellationToken ?? CancellationToken.None);
    }

    private static JsonResponseMessage CreateResponse(HttpResponseMessage response, string? content)
    {
        return new JsonResponseMessage
        {
            StatusCode = response.StatusCode,
            Content = content ?? string.Empty,
            Headers = response.Headers
        };
    }

    private static JsonResponseMessage<TResponse> CreateResponse<TResponse>(HttpResponseMessage response,
        TResponse? content)
        where TResponse : IWebResponse, new()
    {
        return new JsonResponseMessage<TResponse>
        {
            StatusCode = response.StatusCode,
            Content = content ?? new TResponse(),
            Headers = response.Headers
        };
    }
}