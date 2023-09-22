using System.Net;
using Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Provides a response filter that outputs the response in the specified Accept header, or ?format QueryString
/// </summary>
public class ContentNegotiationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var response = await next(context);
        if (response is null)
        {
            return response;
        }

        object value;
        if (response is IResult result)
        {
            if (response is IValueHttpResult valueResult)
            {
                if (valueResult.Value is null)
                {
                    return valueResult;
                }

                value = valueResult.Value; // i.e. Ok(avalue), or NotFound(avalue)
            }
            else
            {
                return result; // i.e. NoContent, or Ok(), or Stream()
            }
        }
        else
        {
            value = response; // a naked object
            if (value is string stringValue)
            {
                if (stringValue.HasNoValue())
                {
                    return Results.NoContent();
                }
            }
        }

        var statusCode = (int)HttpStatusCode.OK;
        if (response is IStatusCodeHttpResult statusCodeResult)
        {
            statusCode = statusCodeResult.StatusCode ?? (int)HttpStatusCode.OK;
        }

        var httpRequest = context.HttpContext.Request;
        var mimeType = NegotiateRequest(httpRequest);
        switch (mimeType)
        {
            case NegotiatedMimeType.Json:
                return Results.Json(value, statusCode: statusCode);
            case NegotiatedMimeType.Xml:
                return new XmlHttpResult<object>(value, statusCode);
            default:
                return Results.StatusCode((int)HttpStatusCode.UnsupportedMediaType);
        }
    }

    private static NegotiatedMimeType? NegotiateRequest(HttpRequest httpRequest)
    {
        var accepts = httpRequest.Headers.Accept;
        httpRequest.Query.TryGetValue(HttpQueryParams.Format, out var formats);

        if (accepts.HasNone() && formats.HasNone())
        {
            return NegotiatedMimeType.Json;
        }

        if (accepts.HasAny())
        {
            if (accepts.Contains(HttpContentTypes.Json))
            {
                return NegotiatedMimeType.Json;
            }

            if (accepts.Contains(HttpContentTypes.Xml))
            {
                return NegotiatedMimeType.Xml;
            }
        }

        if (formats.HasAny())
        {
            if (formats.Contains(HttpContentTypeFormatters.Json))
            {
                return NegotiatedMimeType.Json;
            }

            if (formats.Contains(HttpContentTypeFormatters.Xml))
            {
                return NegotiatedMimeType.Xml;
            }
        }

        return null;
    }

    private enum NegotiatedMimeType
    {
        Json,
        Xml
    }
}