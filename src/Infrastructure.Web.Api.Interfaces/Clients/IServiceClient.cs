using System.Net;
using Application.Interfaces;
using Common;

namespace Infrastructure.Web.Api.Interfaces.Clients;

/// <summary>
///     Defines a service client for calling remote APIs
/// </summary>
public interface IServiceClient : IFireAndForgetServiceClient
{
    Task<Result<string?, ResponseProblem>> DeleteAsync(ICallerContext? context,
        IWebRequest request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null);

    Task<Result<TResponse, ResponseProblem>> GetAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<Result<BinaryResponse, ResponseProblem>> GetBinaryAsync(ICallerContext? context, IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null);

    Task<Result<string?, ResponseProblem>> GetStringAsync(ICallerContext? context, IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null);

    Task<Result<TResponse, ResponseProblem>> PatchAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, PostFile file,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;

    Task<Result<TResponse, ResponseProblem>> PutAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;
}

/// <summary>
///     Defines a response whose content is binary data
/// </summary>
public class BinaryResponse
{
    public required Stream Content { get; set; }

    public long ContentLength { get; set; }

    public required string ContentType { get; set; }

    public HttpStatusCode StatusCode { get; set; }
}

/// <summary>
///     Defines a service client for calling remote APIs that require no response
/// </summary>
public interface IFireAndForgetServiceClient
{
    Task FireAsync(ICallerContext? context, IWebRequestVoid request,
        Action<HttpRequestMessage>? requestFilter = null, CancellationToken? cancellationToken = null);

    Task FireAsync<TResponse>(ICallerContext? context, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter, CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse;
}