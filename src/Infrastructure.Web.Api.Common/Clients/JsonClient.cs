using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Interfaces.Clients;

namespace Infrastructure.Web.Api.Common.Clients;

/// <summary>
///     Provides a convenient typed <see cref="HttpClient" /> that accepts and returns JSON
/// </summary>
[ExcludeFromCodeCoverage]
public class JsonClient : IHttpJsonClient, IDisposable
{
    private readonly HttpClient _client;

    public JsonClient(IHttpClientFactory clientFactory)
    {
        _client = clientFactory.CreateClient();
    }

    public JsonClient(HttpClient client)
    {
        _client = client;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client.Dispose();
        }
    }

    public async Task<JsonResponse> DeleteAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Delete, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> DeleteAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default)
    {
        var response = await SendRequestAsync(HttpMethod.Delete, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> GetAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Get, request, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> GetAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response = await SendRequestAsync(HttpMethod.Get, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PatchAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Patch, request, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> PatchAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response = await SendRequestAsync(HttpMethod.Patch, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PostAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Post, request, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> PostAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response = await SendRequestAsync(HttpMethod.Post, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PutAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse, new()
    {
        var response = await SendRequestAsync(HttpMethod.Put, request, requestFilter, cancellationToken);
        var content = await GetContentAsync<TResponse>(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> PutAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response = await SendRequestAsync(HttpMethod.Put, request, requestFilter, cancellationToken);
        var content = await GetContentAsync(response, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task SendOneWayAsync(IWebRequest request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        await SendRequestAsync(HttpMethod.Put, request, requestFilter, cancellationToken);
    }

    public void SetBaseUrl(string baseUrl)
    {
        _client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, IWebRequest request,
        Action<HttpRequestMessage>? requestFilter, CancellationToken? cancellationToken = default)
    {
        var requestUri = request.GetRequestInfo().Route;
        var content = new StringContent(request.ToJson()!, new MediaTypeHeaderValue(HttpContentTypes.Json));

        return await SendRequestAsync(method, requestUri, content, requestFilter, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string requestUri,
        HttpContent? requestContent, Action<HttpRequestMessage>? requestFilter,
        CancellationToken? cancellationToken = default)
    {
        var request = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(_client.BaseAddress!, requestUri),
            Content = requestContent,
            Headers = { { HttpHeaders.Accept, HttpContentTypes.Json } }
        };
        if (requestFilter is not null)
        {
            requestFilter(request);
        }

        return await _client.SendAsync(request, cancellationToken ?? CancellationToken.None);
    }

    private static async Task<Result<TResponse, ResponseProblem>> GetContentAsync<TResponse>(
        HttpResponseMessage response, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        var contentType = response.Content.Headers.ContentType;
        if (contentType.NotExists())
        {
            return new TResponse();
        }

        if (contentType.MediaType == HttpContentTypes.JsonProblem)
        {
            return await response.Content.ReadFromJsonAsync<ResponseProblem>(
                cancellationToken: cancellationToken ?? CancellationToken.None);
        }

        if (contentType.MediaType == HttpContentTypes.Json)
        {
            return await response.Content.ReadFromJsonAsync<TResponse>(
                cancellationToken: cancellationToken ?? CancellationToken.None) ?? new TResponse();
        }

        return new TResponse();
    }

    private static async Task<Result<string?, ResponseProblem>> GetContentAsync(HttpResponseMessage response,
        CancellationToken? cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType;
        if (contentType.NotExists())
        {
            return default;
        }

        if (contentType.MediaType == HttpContentTypes.JsonProblem)
        {
            return await response.Content.ReadFromJsonAsync<ResponseProblem>(
                cancellationToken: cancellationToken ?? CancellationToken.None);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken ?? CancellationToken.None);
    }

    private static JsonResponse CreateResponse(HttpResponseMessage response, Result<string?, ResponseProblem> content)
    {
        return new JsonResponse
        {
            StatusCode = response.StatusCode,
            Content = content,
            Headers = response.Headers,
            RequestId = response.GetOrCreateRequestId()
        };
    }

    private static JsonResponse<TResponse> CreateResponse<TResponse>(HttpResponseMessage response,
        Result<TResponse, ResponseProblem> content)
        where TResponse : IWebResponse, new()
    {
        return new JsonResponse<TResponse>
        {
            StatusCode = response.StatusCode,
            Content = content,
            Headers = response.Headers,
            RequestId = response.GetOrCreateRequestId()
        };
    }
}