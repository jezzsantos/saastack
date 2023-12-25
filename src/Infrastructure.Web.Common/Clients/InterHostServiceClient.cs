using System.Diagnostics.CodeAnalysis;
using Application.Common;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces.Clients;
using Polly.Retry;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Common.Clients;

/// <summary>
///     A service client used to call between API hosts, with retries
/// </summary>
[ExcludeFromCodeCoverage]
public class InterHostServiceClient : IServiceClient
{
    private const int RetryCount = 2;
    private readonly string _baseUrl;
    private readonly IHttpClientFactory _clientFactory;
    private readonly AsyncRetryPolicy _retryPolicy;

    public InterHostServiceClient(IHttpClientFactory clientFactory, string baseUrl)
    {
        _clientFactory = clientFactory;
        _baseUrl = baseUrl;
        _retryPolicy = ApiClientRetryPolicies.CreateRetryWithExponentialBackoffAndJitter(RetryCount);
    }

    public async Task<Result<string?, ResponseProblem>> DeleteAsync<TResponse>(ICallerContext context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.DeleteAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> GetAsync<TResponse>(ICallerContext context,
        IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.GetAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PatchAsync<TResponse>(ICallerContext context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PatchAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PostAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task<Result<TResponse, ResponseProblem>> PutAsync<TResponse>(ICallerContext context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse, new()
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        return await _retryPolicy.ExecuteAsync(
            async ct => (await client.PutAsync(request, modifiedRequestFilter, ct)).Content,
            cancellationToken ?? CancellationToken.None);
    }

    public async Task FireAsync(ICallerContext context, IWebRequestVoid request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        await _retryPolicy.ExecuteAsync(async ct => await client.SendOneWayAsync(request, modifiedRequestFilter, ct),
            cancellationToken ?? CancellationToken.None);
    }

    public async Task FireAsync<TResponse>(ICallerContext context, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        using var client = CreateJsonClient(context, requestFilter, out var modifiedRequestFilter);
        await _retryPolicy.ExecuteAsync(
            async ct => { await client.SendOneWayAsync(request, modifiedRequestFilter, ct); },
            cancellationToken ?? CancellationToken.None);
    }

    private JsonClient CreateJsonClient(ICallerContext context, Action<HttpRequestMessage>? inboundRequestFilter,
        out Action<HttpRequestMessage> modifiedRequestFilter)
    {
        var client = new JsonClient(_clientFactory);
        client.SetBaseUrl(_baseUrl);
        if (inboundRequestFilter.Exists())
        {
            modifiedRequestFilter = req =>
            {
                inboundRequestFilter(req);
                AddCorrelationId(req, context);
                AddBearerToken(req, context);
            };
        }
        else
        {
            modifiedRequestFilter = req =>
            {
                AddCorrelationId(req, context);
                AddBearerToken(req, context);
            };
        }

        return client;
    }

    private static void AddCorrelationId(HttpRequestMessage message, ICallerContext context)
    {
        message.SetRequestId(context.ToCall());
    }

    private static void AddBearerToken(HttpRequestMessage message, ICallerContext context)
    {
        message.SetBearerToken(context);
    }
}