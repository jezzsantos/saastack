using Application.Interfaces.Services;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

/// <summary>
///     Provides middleware to reverse proxy all (non-hosted) API requests to the Backend API.
///     Ignores any request to a minimal API endpoint
///     Ignores any request that is not to prefix <see cref="WebConstants.BackEndForFrontEndBasePath" />
/// </summary>
public sealed class ReverseProxyMiddleware
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RequestDelegate _next;
    private readonly IHostSettings _settings;
    private string? _backEndApiBaseUrl;

    public ReverseProxyMiddleware(RequestDelegate next, IHostSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        _next = next;
        _settings = settings;
        _httpClientFactory = httpClientFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsBEFFEApiEndpoint(context))
        {
            await _next(context); //Continue down the pipeline
            return;
        }

        var request = context.Request;
        if (IsNotApiRequest(request))
        {
            await _next(context); //Continue down the pipeline
            return;
        }

        if (context.Request.Path.StartsWithSegments(WebConstants.BackEndForFrontEndDocsPath))
        {
            await _next(context); //Continue down the pipeline
            return;
        }

        await ForwardMessageToBackendAsync(context);
    }

    private async Task ForwardMessageToBackendAsync(HttpContext context)
    {
        var request = context.Request;
        var apiUrl = CreateBackendApiUrl(request);
        var httpClient = _httpClientFactory.CreateClient("Forwarder");

        var forwardedMessage = CreateForwardedMessage(request, apiUrl);
        using var response = await httpClient.SendAsync(forwardedMessage,
            HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        {
            await CopyResponseToContextAsync(context, response);
        }
    }

    private static HttpRequestMessage CreateForwardedMessage(HttpRequest request, Uri apiUrl)
    {
        var message = new HttpRequestMessage(new HttpMethod(request.Method), apiUrl);

        if (message.Method != HttpMethod.Get
            && message.Method != HttpMethod.Head
            && message.Method != HttpMethod.Trace
            && message.Method != HttpMethod.Delete)
        {
            var streamContent = new StreamContent(request.Body);
            message.Content = streamContent;
        }

        foreach (var header in request.Headers)
        {
            message.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        foreach (var header in request.Headers)
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        message.Headers.Host = apiUrl.Authority;

        AddAuthentication(request, message);

        return message;
    }

    private static void AddAuthentication(HttpRequest request, HttpRequestMessage message)
    {
        if (request.Cookies.TryGetValue(AuthenticationConstants.Cookies.Token, out var token))
        {
            message.Headers.TryAddWithoutValidation(HttpConstants.Headers.Authorization, $"Bearer {token}");
        }
        else
        {
            message.Headers.Remove(HttpConstants.Headers.Authorization);
        }
    }

    private static async Task CopyResponseToContextAsync(HttpContext context, HttpResponseMessage response)
    {
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");

        await response.Content.CopyToAsync(context.Response.Body);
    }

    private Uri CreateBackendApiUrl(HttpRequest request)
    {
        if (_backEndApiBaseUrl.NotExists())
        {
            //Note: We do this dynamically first time, to support testing and stubbing
            _backEndApiBaseUrl = _settings.GetApiHost1BaseUrl().WithoutTrailingSlash();
        }

        var path = request.GetEncodedPathAndQuery()
            .Replace(WebConstants.BackEndForFrontEndBasePath, string.Empty);

        return new Uri(new Uri(_backEndApiBaseUrl), path);
    }

    private static bool IsNotApiRequest(HttpRequest request)
    {
        return !request.PathBase.ToString().StartsWith(WebConstants.BackEndForFrontEndBasePath);
    }

    private static bool IsBEFFEApiEndpoint(HttpContext context)
    {
        return context.GetEndpoint().Exists();
    }
}