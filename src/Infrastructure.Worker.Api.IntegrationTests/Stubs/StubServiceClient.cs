using Application.Interfaces;
using Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces.Clients;

namespace Infrastructure.Worker.Api.IntegrationTests.Stubs;

public class StubServiceClient : IServiceClient
{
    public Optional<IWebRequest> LastPostedMessage { get; private set; } = Optional<IWebRequest>.None;

    public Task<Result<string?, ResponseProblem>> DeleteAsync(ICallerContext? context,
        IWebRequest request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        throw new NotImplementedException();
    }

    public Task FireAsync(ICallerContext? context, IWebRequestVoid request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        throw new NotImplementedException();
    }

    public Task FireAsync<TResponse>(ICallerContext? context, IWebRequest<TResponse> request,
        Action<HttpRequestMessage>? requestFilter,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        throw new NotImplementedException();
    }

    public Task<Result<TResponse, ResponseProblem>> GetAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        throw new NotImplementedException();
    }

    public Task<Result<BinaryResponse, ResponseProblem>> GetBinaryAsync(ICallerContext? context, IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        throw new NotImplementedException();
    }

    public Task<Result<string?, ResponseProblem>> GetStringAsync(ICallerContext? context, IWebRequest request,
        Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TResponse, ResponseProblem>> PatchAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        throw new NotImplementedException();
    }

    public Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        LastPostedMessage = Optional<IWebRequest>.Some(request);

        var response = Activator.CreateInstance<TResponse>();
        return Task.FromResult<Result<TResponse, ResponseProblem>>(response);
    }

    public Task<Result<TResponse, ResponseProblem>> PostAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, PostFile file, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        throw new NotImplementedException();
    }

    public Task<Result<TResponse, ResponseProblem>> PutAsync<TResponse>(ICallerContext? context,
        IWebRequest<TResponse> request, Action<HttpRequestMessage>? requestFilter = null,
        CancellationToken? cancellationToken = null)
        where TResponse : IWebResponse
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        LastPostedMessage = Optional<IWebRequest>.None;
    }
}