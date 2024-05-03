using System.Net;
using System.Text.Json;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Common.Pipeline;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a response filter that outputs the response in any of these:
///     1. Accept header
///     2. ?format QueryString
///     3. Format field of a JSON request body
/// </summary>
public class ContentNegotiationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var response = await next(context); //Continue down the pipeline
        if (response is null)
        {
            return response;
        }

        object responseValue;
        var isProblemValue = false;
        if (response is IResult result)
        {
            CopyLocationHeader(result, context.HttpContext);

            if (response is IValueHttpResult valueResult)
            {
                if (valueResult.Value is null)
                {
                    return valueResult;
                }

                responseValue = valueResult
                    .Value; // i.e. Ok(avalue), or NotFound(avalue), or Created(avalue), or Accepted(avalue) or Problem(avalue)

                isProblemValue = responseValue is ProblemDetails;
            }
            else
            {
                return result; // i.e. NoContent, or Ok(), or Created(uri), or Accepted(uri), or Stream() or Problem()
            }
        }
        else
        {
            responseValue = response; // a naked object

            if (responseValue is string stringValue)
            {
                if (stringValue.HasNoValue())
                {
                    return Results.NoContent();
                }
            }
        }

        var responseStatusCode = (int)HttpStatusCode.OK;
        if (response is IStatusCodeHttpResult statusCodeResult)
        {
            responseStatusCode = statusCodeResult.StatusCode ?? (int)HttpStatusCode.OK;
        }

        var httpRequest = context.HttpContext.Request;
        var requestedContent = await GetRequestedContentAsync(httpRequest, context.HttpContext.RequestAborted);

        switch (requestedContent)
        {
            case NegotiatedMimeType.Json:
            {
                var contentType = isProblemValue
                    ? HttpConstants.ContentTypes.JsonProblem
                    : null;
                return Results.Json(responseValue, statusCode: responseStatusCode, contentType: contentType);
            }

            case NegotiatedMimeType.Xml:
            {
                var contentType = isProblemValue
                    ? HttpConstants.ContentTypes.XmlProblem
                    : null;
                return new XmlHttpResult<object>(responseValue, responseStatusCode, contentType);
            }

            default:
            {
                return Results.StatusCode((int)HttpStatusCode.UnsupportedMediaType);
            }
        }
    }

    private static async Task<NegotiatedMimeType?> GetRequestedContentAsync(HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var accepts = httpRequest.Headers.Accept;
        var queries = GetFormatInQueryString(httpRequest);
        var bodyField = await GetFormatFromBodyAsync(httpRequest, cancellationToken);

        if (accepts.HasNone() && queries.HasNone() && bodyField.HasNoValue())
        {
            return NegotiatedMimeType.Json;
        }

        if (accepts.HasAny())
        {
            if (accepts.ContainsIgnoreCase(HttpConstants.ContentTypes.Everything))
            {
                return NegotiatedMimeType.Json;
            }

            if (accepts.ContainsIgnoreCase(HttpConstants.ContentTypes.Json))
            {
                return NegotiatedMimeType.Json;
            }

            if (accepts.ContainsIgnoreCase(HttpConstants.ContentTypes.Xml))
            {
                return NegotiatedMimeType.Xml;
            }
        }

        if (queries.HasAny())
        {
            if (queries.ContainsIgnoreCase(HttpConstants.ContentTypeFormatters.Json))
            {
                return NegotiatedMimeType.Json;
            }

            if (queries.ContainsIgnoreCase(HttpConstants.ContentTypeFormatters.Xml))
            {
                return NegotiatedMimeType.Xml;
            }
        }

        if (bodyField.HasValue())
        {
            if (bodyField.EqualsIgnoreCase(HttpConstants.ContentTypeFormatters.Json))
            {
                return NegotiatedMimeType.Json;
            }

            if (bodyField.EqualsIgnoreCase(HttpConstants.ContentTypeFormatters.Xml))
            {
                return NegotiatedMimeType.Xml;
            }
        }

        return null;
    }

    private static async Task<string?> GetFormatFromBodyAsync(HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        if (httpRequest.HasFormContentType)
        {
            return GetFormatInFormData(httpRequest).FirstOrDefault();
        }

        if (httpRequest.HasJsonContentType())
        {
            return (await GetFormatInJsonPayloadAsync(httpRequest, cancellationToken)).FirstOrDefault();
        }

        return default;
    }

    private static async Task<StringValues> GetFormatInJsonPayloadAsync(HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        if (httpRequest.Body.Position != 0)
        {
            httpRequest.RewindBody();
        }

        try
        {
            var requestWithFormat =
                (RequestWithFormat?)await httpRequest.ReadFromJsonAsync(typeof(RequestWithFormat), cancellationToken);
            if (requestWithFormat is not null && requestWithFormat.Format.HasValue())
            {
                return new StringValues(requestWithFormat.Format);
            }

            httpRequest.RewindBody();
        }
        catch (JsonException)
        {
            return StringValues.Empty;
        }

        return StringValues.Empty;
    }

    private static StringValues GetFormatInFormData(HttpRequest httpRequest)
    {
        return httpRequest.Form.TryGetValue(HttpConstants.QueryParams.Format, out var formats)
            ? formats
            : StringValues.Empty;
    }

    private static StringValues GetFormatInQueryString(HttpRequest httpRequest)
    {
        return httpRequest.Query.TryGetValue(HttpConstants.QueryParams.Format, out var formats)
            ? formats
            : StringValues.Empty;
    }

    private static void CopyLocationHeader(IResult result, HttpContext httpContext)
    {
        if (result is Created created)
        {
            if (created.Location.HasValue())
            {
                httpContext.Response.Headers.Location = created.Location;
                return;
            }
        }

        if (result is Accepted accepted)
        {
            if (accepted.Location.HasValue())
            {
                httpContext.Response.Headers.Location = accepted.Location;
                return;
            }
        }

        var resultType = result.GetType();

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Created<>))
        {
            var location = (string?)resultType.GetProperty(nameof(Created<object>.Location))!.GetValue(result);
            if (location.HasValue())
            {
                httpContext.Response.Headers.Location = location;
                return;
            }
        }

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Accepted<>))
        {
            var location = (string?)resultType.GetProperty(nameof(Accepted<object>.Location))!.GetValue(result);
            if (location.HasValue())
            {
                httpContext.Response.Headers.Location = location;
            }
        }
    }

    private enum NegotiatedMimeType
    {
        Json,
        Xml
    }

    // ReSharper disable once MemberCanBePrivate.Global
    internal class RequestWithFormat
    {
        public string? Format { get; set; }
    }
}