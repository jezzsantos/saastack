using System.Net;
using System.Net.Http.Headers;
using Common;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Interfaces.Clients;

/// <summary>
///     Defines a JSON response
/// </summary>
public class JsonResponse : IDisposable
{
    public Result<string?, ResponseProblem> Content { get; init; }

    public required HttpResponseHeaders Headers { get; set; }

    public required string RequestId { get; init; }

    public required HttpStatusCode StatusCode { get; init; }

    public Stream? RawContent { get; set; }

    public void Dispose()
    {
        RawContent?.Dispose();
    }
}

/// <summary>
///     Defines a JSON response of the specified <see cref="TResponse" />
/// </summary>
public class JsonResponse<TResponse> : JsonResponse
    where TResponse : IWebResponse
{
    public new required Result<TResponse, ResponseProblem> Content { get; init; }
}