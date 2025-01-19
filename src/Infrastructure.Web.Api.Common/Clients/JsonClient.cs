using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using JsonException = System.Text.Json.JsonException;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Api.Common.Clients;

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
        var content = await GetStringResponseAsync(response, cancellationToken);

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
        var content = await GetStringResponseAsync(response, cancellationToken);

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
        var content = await GetStringResponseAsync(response, cancellationToken);

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
        var content = await GetStringResponseAsync(response, cancellationToken);

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
        var content = await GetStringResponseAsync(response, cancellationToken);

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
        var content = await GetStringResponseAsync(response, cancellationToken);

        return CreateResponse(response, content);
    }

    internal static async Task<Result<string?, ResponseProblem>> GetStringResponseAsync(HttpResponseMessage response,
        CancellationToken? cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var contentType = response.Content.Headers.ContentType;
            if (contentType.NotExists())
            {
                return default;
            }

            switch (contentType.MediaType)
            {
                case HttpConstants.ContentTypes.Text or HttpConstants.ContentTypes.Html:
                    return await response.Content.ReadAsStringAsync(cancellationToken ?? CancellationToken.None);

                case HttpConstants.ContentTypes.OctetStream:
                    return default;

                case HttpConstants.ContentTypes.Json:
                    return await response.Content.ReadAsStringAsync(cancellationToken ?? CancellationToken.None);

                default:
                {
                    //Unrecognized content type (could be a file or image?)
                    return default;
                }
            }
        }

        return await ParseErrorAsync(response, cancellationToken ?? CancellationToken.None);
    }

    internal static async Task<Result<TResponse, ResponseProblem>> GetTypedResponseAsync<TResponse>(
        HttpResponseMessage response, JsonSerializerOptions? jsonOptions, CancellationToken? cancellationToken)
        where TResponse : IWebResponse
    {
        if (response.IsSuccessStatusCode)
        {
            var contentType = response.Content.Headers.ContentType;
            if (contentType.NotExists())
            {
                if (typeof(EmptyResponse).IsAssignableTo(typeof(TResponse)))
                {
                    return TryCreateEmptyResponse<TResponse>();
                }

                // Assumes JSON by default
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

            switch (contentType.MediaType)
            {
                case HttpConstants.ContentTypes.Text or HttpConstants.ContentTypes.Html:
                {
                    return TryCreateEmptyResponse<TResponse>();
                }

                case HttpConstants.ContentTypes.OctetStream:
                    return default;

                case HttpConstants.ContentTypes.Json:
                {
                    var instance = await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions,
                        cancellationToken ?? CancellationToken.None);
                    return instance.Exists()
                        ? instance
                        : TryCreateEmptyResponse<TResponse>();
                }

                default:
                {
                    //Unrecognized content type
                    return HttpStatusCode.UnsupportedMediaType.ToResponseProblem(
                        string.Format(Resources.JsonClient_GetTypedResponse_UnsupportedMediaType,
                            contentType.MediaType));
                }
            }
        }

        return await ParseErrorAsync(response, cancellationToken ?? CancellationToken.None);
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

                multipart.Add(streamContent, file.PartName, file.Filename ?? file.PartName);
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

    private static async Task<ResponseProblem> ParseErrorAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        var errorText = response.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();

        if (TryParseRfc7807Error(errorText, response.StatusCode, out var problem1))
        {
            return problem1;
        }

        if (TryParseRfc6749Error(errorText, response.StatusCode, out var problem2))
        {
            return problem2;
        }

        if (TryParseNonStandardErrors(errorText, response.StatusCode, out var problem3))
        {
            return problem3;
        }

        return response.StatusCode.ToResponseProblem(response.ReasonPhrase);
    }

    private static bool TryParseRfc7807Error(string responseText, HttpStatusCode statusCode,
        out ResponseProblem problem)
    {
        problem = new ResponseProblem();

        try
        {
            var details = responseText.FromJson<ProblemDetails>();
            if (details.NotExists()
                || (details.Title.HasNoValue() && details.Detail.HasNoValue()))
            {
                return false;
            }

            problem = details.ToResponseProblem();
            problem.Status = (int)statusCode;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseRfc6749Error(string responseText, HttpStatusCode statusCode,
        out ResponseProblem problem)
    {
        problem = new ResponseProblem();
        try
        {
            var details = responseText.FromJson<OAuth2Rfc6749ProblemDetails>();
            if (details.NotExists() || details.Error.HasNoValue())
            {
                return false;
            }

            problem = details.ToResponseProblem((int)statusCode);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseNonStandardErrors(string responseText, HttpStatusCode statusCode,
        out ResponseProblem problem)
    {
        problem = new ResponseProblem();
        try
        {
            var details = responseText.FromJson<NonStandardProblemDetails>();
            if (details.NotExists())
            {
                return false;
            }

            if (details.Error.Exists())
            {
                // Google errors: https://google.github.io/styleguide/jsoncstyleguide.xml
                if (details.Error.Code.HasValue())
                {
                    problem = statusCode.ToResponseProblem(details.Error.Code, details.Error.Message);
                    return true;
                }

                // Other random formats
                if (details.Error.Reason.HasValue())
                {
                    problem = statusCode.ToResponseProblem(details.Error.Reason, details.Error.Description);
                    return true;
                }
            }

            // Other common formats
            if (details.Status.HasValue && details.Details.HasValue())
            {
                problem = statusCode.ToResponseProblem(details.Details);
                return true;
            }

            // Mailgun API formats
            if (details.Message.HasValue())
            {
                problem = statusCode.ToResponseProblem(details.Message);
                return true;
            }

            problem = statusCode.ToResponseProblem(Resources.JsonClient_TryParseNonStandardErrors_NonStandard,
                responseText);
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

/// <summary>
///     Defines non-standard 3rd party errors
///     Note: We are attempting to model all possible error formats we have encountered in the wild
/// </summary>
[UsedImplicitly]
internal class NonStandardProblemDetails
{
    [JsonPropertyName("message")] public string? Message { get; set; }
    
    [JsonPropertyName("details")] public string? Details { get; set; }

    [JsonPropertyName("error")] public NonStandardProblemError? Error { get; set; }

    [JsonPropertyName("status")] public int? Status { get; set; }
}

/// <summary>
///     Defines a non-standard 3rd party error
///     Note: We are attempting to model all possible error formats we have encountered in the wild
/// </summary>
[UsedImplicitly]
internal class NonStandardProblemError
{
    [JsonPropertyName("code")] public string? Code { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("message")] public string? Message { get; set; }

    [JsonPropertyName("reason")] public string? Reason { get; set; }
}