using System.Net;
using System.Net.Http.Headers;
using Common;

namespace Infrastructure.Web.Api.Interfaces.Clients;

/// <summary>
///     Defines a JSON response
/// </summary>
public class JsonResponse
{
    public Result<string?, ResponseProblem> Content { get; init; }

    public required HttpResponseHeaders Headers { get; set; }

    public required string RequestId { get; init; }

    public required HttpStatusCode StatusCode { get; init; }
}

public class JsonResponse<TResponse> : JsonResponse
    where TResponse : IWebResponse
{
    public new required Result<TResponse, ResponseProblem> Content { get; init; }
}