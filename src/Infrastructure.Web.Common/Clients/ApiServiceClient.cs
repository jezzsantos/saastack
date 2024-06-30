using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces.Clients;
using Polly.Retry;

namespace Infrastructure.Web.Common.Clients;

/// <summary>
///     A service client used to call external 3rd party services, with retries
/// </summary>
[ExcludeFromCodeCoverage]
public class ApiServiceClient : IServiceClient
{
    private const int RetryCount = 1;
    protected readonly string BaseUrl;
    protected readonly IHttpClientFactory ClientFactory;
    protected readonly JsonSerializerOptions JsonOptions;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ApiServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl) : this(
        clientFactory, jsonOptions, baseUrl, RetryCount)
    {
    }

    protected ApiServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl,
        int retryCount)
    {
        ClientFactory = clientFactory;
        JsonOptions = jsonOptions;
        BaseUrl = baseUrl;
        _retryPolicy = ApiClientRetryPolicies.CreateRetryWithExponentialBackoffAndJitter(retryCount);
    }

    public async Task<Result<string?, ResponseProblem>> DeleteAsync(ICallerContext? context,
        IWebRequest request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.DeleteAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task FireAsync(ICallerContext? context, IWebRequestVoid request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        await _retryPolicy.ExecuteAsync(async ct => await client.SendOneWayAsync(request, modifiedRequestFilter, ct),
            cancellationToken ?? CancellationToken.None);
    }

    public async Task FireAsync<TResponse>(ICallerContext? context, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        await _retryPolicy.ExecuteAsync(
            async ct => { await client.SendOneWayAsync(request, modifiedRequestFilter, ct); },
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> GetAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.GetAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<BinaryResponse, ResponseProblem>> GetBinaryAsync(ICallerContext? context,
        IWebRequest request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct =>
            {
                var response = await client.GetAsync(request, modifiedRequestFilter, ct);
                return new BinaryResponse
                {
                    Content = response.RawContent!,
                    ContentType = response.ContentHeaders.ContentType?.MediaType!,
                    ContentLength = response.ContentHeaders.ContentLength!.Value,
                    StatusCode = response.StatusCode
                };
            },
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<string?, ResponseProblem>> GetStringAsync(ICallerContext? context, IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.GetAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PatchAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PatchAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PostAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PutAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PutAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    protected virtual JsonClient CreateJsonClient(ICallerContext? context,
        Action<HttpRequestMessage>? inboundRequestFilter,
        out Action<HttpRequestMessage> modifiedRequestFilter)
    {
        var client = new JsonClient(ClientFactory, JsonOptions);
        client.SetBaseUrl(BaseUrl);
        if (inboundRequestFilter.Exists())
        {
            modifiedRequestFilter = inboundRequestFilter;
        }
        else
        {
            modifiedRequestFilter = _ => { };
        }

        return client;
    }
}