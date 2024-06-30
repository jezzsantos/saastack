using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace IntegrationTesting.WebApi.Common;

/// <summary>
///     Provides a <see cref="IHttpClient" /> used for testing
/// </summary>
public sealed class TestingClient : IHttpClient, IDisposable
{
    private readonly CookieContainerHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public TestingClient(HttpClient httpClient, JsonSerializerOptions jsonOptions, CookieContainerHandler handler)
    {
        _httpClient = httpClient;
        _jsonOptions = jsonOptions;
        _handler = handler;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    public Uri? BaseAddress => _httpClient.BaseAddress;

    public async Task<HttpResponseMessage> GetAsync(string route,
        Action<HttpRequestMessage, CookieContainer>? requestFilter = null)
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(BaseAddress!, route)
        };

        return await SendAsync(message, requestFilter);
    }

    public async Task<string> GetStringAsync(string route,
        Action<HttpRequestMessage, CookieContainer>? requestFilter = null)
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(BaseAddress!, route)
        };

        var response = await SendAsync(message, requestFilter);

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<HttpResponseMessage> PostAsync(string route, HttpContent content,
        Action<HttpRequestMessage, CookieContainer>? requestFilter = null)
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(BaseAddress!, route),
            Content = content
        };

        return await SendAsync(message, requestFilter);
    }

    public async Task<HttpResponseMessage> PostEmptyJsonAsync(string route,
        Action<HttpRequestMessage, CookieContainer>? requestFilter = null)
    {
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(BaseAddress!, route),
            Content = JsonContent.Create(new { })
        };

        return await SendAsync(message, requestFilter);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message,
        Action<HttpRequestMessage, CookieContainer>? requestFilter = null)
    {
        if (requestFilter.Exists())
        {
            requestFilter(message, _handler.Container);
        }

        var response = await _httpClient.SendAsync(message);
        if (response.StatusCode >= HttpStatusCode.InternalServerError)
        {
            throw await ToExceptionAsync(response, _jsonOptions);
        }

        return response;
    }

    private static async Task<Exception> ToExceptionAsync(HttpResponseMessage response,
        JsonSerializerOptions jsonOptions)
    {
        var problem = await response.AsProblemAsync(jsonOptions);
        if (problem.Exists())
        {
            var details = problem.ToResponseProblem();
            throw details.ToException();
        }

        throw response.StatusCode.ToResponseProblem(response.ReasonPhrase)
            .ToException();
    }
}