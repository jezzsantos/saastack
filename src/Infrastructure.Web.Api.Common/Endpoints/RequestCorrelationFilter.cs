using Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a request/response filter that accepts (or creates) a request correlation ID,
///     and then includes it in the response
/// </summary>
public class RequestCorrelationFilter : IEndpointFilter
{
    public const string CorrelationIdItemName = "_correlationId";
    public const string ResponseHeaderName = HttpHeaders.RequestId;

    public static readonly string[] AcceptedRequestHeaderNames =
        { "Correlation-Id", "X-Correlation-Id", HttpHeaders.RequestId, "X-Request-ID" };

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!GetFromRequestPipeline(context.HttpContext, out var correlationId))
        {
            if (!GetFromRequestHeaders(context, out correlationId))
            {
                correlationId = Caller.GenerateCallId();
            }

            SaveToRequestPipeline(context.HttpContext, correlationId);
        }

        var response = await next(context); //Continue down the pipeline

        SetOnResponse(context.HttpContext, correlationId);

        return response;
    }

    private static bool GetFromRequestPipeline(HttpContext httpContext, out object? correlationId)
    {
        var items = httpContext.Items;
        return items.TryGetValue(CorrelationIdItemName, out correlationId);
    }

    private static bool GetFromRequestHeaders(EndpointFilterInvocationContext context, out object? correlationId)
    {
        correlationId = null;
        foreach (var header in AcceptedRequestHeaderNames)
        {
            if (context.HttpContext.Request.Headers.TryGetValue(header, out var headerValues))
            {
                correlationId = headerValues.FirstOrDefault();
                return true;
            }
        }

        return false;
    }

    private static void SaveToRequestPipeline(HttpContext httpContext, object? correlationId)
    {
        httpContext.Items.Add(CorrelationIdItemName, correlationId);
    }

    private static void SetOnResponse(HttpContext httpContext, object? correlationId)
    {
        httpContext.Response.Headers[ResponseHeaderName] = new StringValues(correlationId!.ToString());
    }
}