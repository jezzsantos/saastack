using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces.Clients;
using Microsoft.AspNetCore.Mvc;
using JsonException = System.Text.Json.JsonException;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Common.Clients;

/// <summary>
///     Provides a convenient typed <see cref="HttpClient" /> that accepts and returns JSON
/// </summary>
[ExcludeFromCodeCoverage]
public class JsonClient : IHttpJsonClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions) : this(
        clientFactory.CreateClient(), jsonOptions)
    {
    }

    public JsonClient(HttpClient httpClient, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _jsonOptions = jsonOptions;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient.Dispose();
        }
    }

    public async Task<JsonResponse<TResponse>> DeleteAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse
    {
        var response = await SendRequestAsync(_httpClient, HttpMethod.Delete, request, null, requestFilter,
            cancellationToken);
        var content = await GetTypedResponseAsync<TResponse>(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> DeleteAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = default)
    {
        var response = await SendRequestAsync(_httpClient, HttpMethod.Delete, request, null, requestFilter,
            cancellationToken);
        var content = await GetStringResponseAsync(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> GetAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Get, request, null, requestFilter, cancellationToken);
        var content = await GetTypedResponseAsync<TResponse>(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> GetAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Get, request, null, requestFilter, cancellationToken);
        var content = await GetStringResponseAsync(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PatchAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse
    {
        var response = await SendRequestAsync(_httpClient, HttpMethod.Patch, request, null, requestFilter,
            cancellationToken);
        var content = await GetTypedResponseAsync<TResponse>(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> PatchAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response = await SendRequestAsync(_httpClient, HttpMethod.Patch, request, null, requestFilter,
            cancellationToken);
        var content = await GetStringResponseAsync(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PostAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Post, request, null, requestFilter, cancellationToken);
        var content = await GetTypedResponseAsync<TResponse>(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> PostAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Post, request, null, requestFilter, cancellationToken);
        var content = await GetStringResponseAsync(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PostAsync<TResponse>(IWebRequest<TResponse> request, PostFile file,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Post, request, file, requestFilter, cancellationToken);
        var content = await GetTypedResponseAsync<TResponse>(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> PostAsync(IWebRequest request, PostFile file,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Post, request, file, requestFilter, cancellationToken);
        var content = await GetStringResponseAsync(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PutAsync<TResponse>(IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Put, request, null, requestFilter, cancellationToken);
        var content = await GetTypedResponseAsync<TResponse>(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse<TResponse>> PutAsync<TResponse>(IWebRequest<TResponse> request, PostFile file,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
        where TResponse : IWebResponse
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Put, request, file, requestFilter, cancellationToken);
        var content = await GetTypedResponseAsync<TResponse>(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    public async Task<JsonResponse> PutAsync(IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        var response =
            await SendRequestAsync(_httpClient, HttpMethod.Put, request, null, requestFilter, cancellationToken);
        var content = await GetStringResponseAsync(response, _jsonOptions, cancellationToken);

        return CreateResponse(response, content);
    }

    internal static async Task<Result<string?, ResponseProblem>> GetStringResponseAsync(HttpResponseMessage response,
        JsonSerializerOptions? jsonOptions, CancellationToken? cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType;
        if (contentType.NotExists())
        {
            if (response.IsSuccessStatusCode)
            {
                return default;
            }

            return response.StatusCode.ToResponseProblem(response.ReasonPhrase);
        }

        if (contentType.MediaType == HttpConstants.ContentTypes.JsonProblem)
        {
            if (TryReadRfc7807Error(response, jsonOptions, cancellationToken, out var problem))
            {
                return problem;
            }
        }

        if (contentType.MediaType == HttpConstants.ContentTypes.Json)
        {
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken ?? CancellationToken.None);
            }

            if (response.Content.Headers.ContentType.Exists())
            {
                if (TryReadRfc6749Error(response, jsonOptions, cancellationToken, out var problem))
                {
                    return problem;
                }
            }

            return response.StatusCode.ToResponseProblem(response.ReasonPhrase);
        }

        if (contentType.MediaType is HttpConstants.ContentTypes.Text or HttpConstants.ContentTypes.Html)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken ?? CancellationToken.None);
        }

        if (contentType.MediaType == HttpConstants.ContentTypes.OctetStream)
        {
            return default;
        }

        //Unrecognized content type (could be a file or image?)
        return default;
    }

    internal static async Task<Result<TResponse, ResponseProblem>> GetTypedResponseAsync<TResponse>(
        HttpResponseMessage response, JsonSerializerOptions? jsonOptions, CancellationToken? cancellationToken)
        where TResponse : IWebResponse
    {
        var contentType = response.Content.Headers.ContentType;
        if (contentType.NotExists())
        {
            if (response.IsSuccessStatusCode)
            {
                if (typeof(EmptyResponse).IsAssignableTo(typeof(TResponse)))
                {
                    return TryCreateEmptyResponse<TResponse>();
                }

                // Assume JSON
                try
                {
                    var instance = await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions,
                        cancellationToken ?? CancellationToken.None);
                    return instance.Exists()
                        ? instance
                        : TryCreateEmptyResponse<TResponse>();
                }
                catch (JsonException)
                {
                    return TryCreateEmptyResponse<TResponse>();
                }
            }

            return response.StatusCode.ToResponseProblem(response.ReasonPhrase);
        }

        if (contentType.MediaType is HttpConstants.ContentTypes.JsonProblem)
        {
            if (TryReadRfc7807Error(response, jsonOptions, cancellationToken, out var problem))
            {
                return problem;
            }
        }

        if (contentType.MediaType is HttpConstants.ContentTypes.Json)
        {
            if (response.IsSuccessStatusCode)
            {
                var instance = await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions,
                    cancellationToken ?? CancellationToken.None);
                return instance.Exists()
                    ? instance
                    : TryCreateEmptyResponse<TResponse>();
            }

            if (response.Content.Headers.ContentType.Exists())
            {
                if (TryReadRfc6749Error(response, jsonOptions, cancellationToken, out var problem))
                {
                    return problem;
                }
            }

            return response.StatusCode.ToResponseProblem(response.ReasonPhrase);
        }

        if (contentType.MediaType is HttpConstants.ContentTypes.Text or HttpConstants.ContentTypes.Html)
        {
            if (response.IsSuccessStatusCode)
            {
                return TryCreateEmptyResponse<TResponse>();
            }

            return response.StatusCode.ToResponseProblem(response.ReasonPhrase);
        }

        return HttpStatusCode.UnsupportedMediaType.ToResponseProblem(
            string.Format(Resources.JsonClient_GetTypedResponse_UnsupportedMediaType, contentType.MediaType));
    }

    public async Task SendOneWayAsync(IWebRequest request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = default)
    {
        await SendRequestAsync(_httpClient, HttpMethod.Put, request, null, requestFilter, cancellationToken);
    }

    internal static async Task<HttpResponseMessage> SendRequestAsync(HttpClient httpClient, HttpMethod method,
        IWebRequest request, PostFile? file, Action<HttpRequestMessage>? requestFilter,
        CancellationToken? cancellationToken = default)
    {
        var (info, body) = request.ParseRequestInfo();
        var requestUri = info.Route;

        HttpContent? content = null;
        try
        {
            if (method.CanHaveBody() && file.Exists())
            {
                var multipart = ToMultiPartContent(body);
                var streamContent = new StreamContent(file.Stream);
                if (file.ContentType.HasValue())
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                }

                multipart.Add(streamContent, "file", file.Filename);
                content = multipart;
            }
            else if (method.CanHaveBody() && request is IHasMultipartFormData)
            {
                var multipart = ToMultiPartContent(body);
                content = multipart;
            }
            else if (method.CanHaveBody() && request is IHasFormUrlEncoded)
            {
                var urlEncoded = ToUrlEncodedContent(body);
                content = urlEncoded;
            }
            else
            {
                var json = body.SerializeToJson();
                content = method.CanHaveBody()
                    ? new StringContent(json,
                        new MediaTypeHeaderValue(HttpConstants.ContentTypes.Json))
                    : null;
            }

            return await SendRequestAsync(httpClient, method, requestUri, content, requestFilter, cancellationToken);
        }
        finally
        {
            (content as IDisposable)?.Dispose();
        }
    }

    public void SetBaseUrl(string baseUrl)
    {
        _httpClient.BaseAddress = new Uri(baseUrl.WithTrailingSlash(), UriKind.Absolute);
    }

    /// <summary>
    ///     Converts the request body into a <see cref="FormUrlEncodedContent" />.
    ///     Properties of the <see cref="body" /> that are arrays are expanded into multiple individual params with the same
    ///     name. e.g. Tags=Value1&amp;Tags=Value2
    ///     Properties of the <see cref="body" /> that are objects/dictionaries are expanded into multiple individual params
    ///     with the different indexed names. e.g. Tags[Key1]=Value1&amp;Tags[Key2]=Value2
    /// </summary>
    private static FormUrlEncodedContent ToUrlEncodedContent(IWebRequest body)
    {
        var requestFields = body.SerializeToJson()
            .FromJson<Dictionary<string, object>>()!;
        if (requestFields.HasNone())
        {
            return new FormUrlEncodedContent(new Dictionary<string, string>());
        }

        var values = new Dictionary<string, string>();
        foreach (var field in requestFields)
        {
            if (field.Value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.Value.ToString();
                        values.Add($"{field.Key}[{propertyName}]", propertyValue);
                    }
                }
                else if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        var itemValue = item.ToString();
                        values.Add(field.Key, itemValue);
                    }
                }
                else
                {
                    var value = jsonElement.ToString();
                    values.Add(field.Key, value);
                }
            }
            else
            {
                var value = field.Value.ToString() ?? string.Empty;
                values.Add(field.Key, value);
            }
        }

        return new FormUrlEncodedContent(values);
    }

    /// <summary>
    ///     Converts request body into a <see cref="MultipartFormDataContent" />.
    ///     Properties of the <see cref="body" /> that are arrays are expanded into multiple individual parts with the same
    ///     name. e.g. Tags=Value1 and Tags=Value2
    ///     Properties of the <see cref="body" /> that are objects/dictionaries are expanded into multiple individual parts
    ///     with different indexed names. e.g. Tags[Key1]=Value1 and Tags[Key2]=Value2
    /// </summary>
    private static MultipartFormDataContent ToMultiPartContent(IWebRequest body)
    {
        var content = new MultipartFormDataContent();
        var requestFields = body.SerializeToJson()
            .FromJson<Dictionary<string, object>>()!;
        if (requestFields.HasAny())
        {
            foreach (var field in requestFields)
            {
                if (field.Value is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in jsonElement.EnumerateObject())
                        {
                            var propertyName = property.Name;
                            content.Add(CreateStringContent(property), $"{field.Key}[{propertyName}]");
                        }
                    }
                    else if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            content.Add(CreateStringContent(item), field.Key);
                        }
                    }
                    else
                    {
                        content.Add(CreateStringContent(field.Value), field.Key);
                    }
                }
                else
                {
                    content.Add(CreateStringContent(field.Value), field.Key);
                }
            }
        }

        return content;

        static StringContent CreateStringContent(object value)
        {
            var stringValue = value.ToString() ?? string.Empty;
            return new StringContent(stringValue);
        }
    }

    private static Result<TResponse, ResponseProblem> TryCreateEmptyResponse<TResponse>()
        where TResponse : IWebResponse
    {
        try
        {
            return Activator.CreateInstance<TResponse>();
        }
        catch (Exception ex)
        {
            return HttpStatusCode.InternalServerError.ToResponseProblem(
                string.Format(Resources.JsonClient_TryCreateEmptyResponse_NotConstructable, typeof(TResponse),
                    ex.Message));
        }
    }

    private static async Task<HttpResponseMessage> SendRequestAsync(HttpClient httpClient, HttpMethod method,
        string requestUri, HttpContent? requestContent, Action<HttpRequestMessage>? requestFilter,
        CancellationToken? cancellationToken = default)
    {
        var request = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(httpClient.BaseAddress!, requestUri.WithoutLeadingSlash()),
            Content = requestContent,
            Headers = { { HttpConstants.Headers.Accept, HttpConstants.ContentTypes.Json } }
        };
        if (requestFilter is not null)
        {
            requestFilter(request);
        }

        return await httpClient.SendAsync(request, cancellationToken ?? CancellationToken.None);
    }

    private static bool TryReadRfc7807Error(HttpResponseMessage response, JsonSerializerOptions? jsonOptions,
        CancellationToken? cancellationToken, out ResponseProblem problem)
    {
        if (cancellationToken.HasValue)
        {
            cancellationToken.Value.ThrowIfCancellationRequested();
        }

        problem = new ResponseProblem();

        try
        {
            var details = response.Content.ReadFromJsonAsync<ProblemDetails>(jsonOptions, CancellationToken.None)
                .GetAwaiter().GetResult()!;
            if (details.Type.HasNoValue())
            {
                return false;
            }

            problem = details.ToResponseProblem();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadRfc6749Error(HttpResponseMessage response, JsonSerializerOptions? jsonOptions,
        CancellationToken? cancellationToken, out ResponseProblem problem)
    {
        if (cancellationToken.HasValue)
        {
            cancellationToken.Value.ThrowIfCancellationRequested();
        }

        problem = new ResponseProblem();
        try
        {
            var details = response.Content
                .ReadFromJsonAsync<OAuth2Rfc6749ProblemDetails>(jsonOptions, CancellationToken.None)
                .GetAwaiter().GetResult()!;
            if (details.Error.HasNoValue())
            {
                return false;
            }

            problem = details.ToResponseProblem((int)response.StatusCode);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonResponse CreateResponse(HttpResponseMessage response, Result<string?, ResponseProblem> content)
    {
        return new JsonResponse
        {
            StatusCode = response.StatusCode,
            Content = content,
            ContentHeaders = response.Content.Headers,
            Headers = response.Headers,
            RequestId = response.GetOrCreateRequestId(),
            RawContent = content is { IsSuccessful: true, HasValue: false }
                ? response.Content.ReadAsStream()
                : null
        };
    }

    private static JsonResponse<TResponse> CreateResponse<TResponse>(HttpResponseMessage response,
        Result<TResponse, ResponseProblem> content)
        where TResponse : IWebResponse
    {
        return new JsonResponse<TResponse>
        {
            StatusCode = response.StatusCode,
            Content = content,
            ContentHeaders = response.Content.Headers,
            Headers = response.Headers,
            RequestId = response.GetOrCreateRequestId(),
            RawContent = content is { IsSuccessful: true, HasValue: false }
                ? response.Content.ReadAsStream()
                : null
        };
    }
}