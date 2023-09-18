using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Serialization;
using Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Provides a response filter that outputs the response in the specified Accept header
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

                value = valueResult.Value; // i.e. Ok(somecontent), or NotFound(somecontent)
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
        string contentType;
        await using var
            content = new MemoryStream(); // HACK: must be a better perf way to do this, so that we dont allocate so much memory?
        switch (mimeType)
        {
            case NegotiatedMimeType.Json:
                contentType = await ConvertToJsonAsync(value, context, content);
                break;
            case NegotiatedMimeType.Xml:
                contentType = await ConvertToXmlAsync(value, context, content);
                break;
            default:
                return Results.StatusCode((int)HttpStatusCode.UnsupportedMediaType);
        }

        content.Position = 0;
        using var streamReader = new StreamReader(content, Encoding.UTF8);
        var textContent = await streamReader.ReadToEndAsync();

        return Results.Content(textContent, contentType, Encoding.UTF8, statusCode);
    }

    private static async Task<string> ConvertToJsonAsync(object value, EndpointFilterInvocationContext context,
        Stream content)
    {
        var resultType = value.GetType();
        var jsonOptions = GetJsonOptionsFromRequest(context.HttpContext);
        await JsonSerializer.SerializeAsync(content, value, resultType, jsonOptions,
            CancellationToken.None);

        return HttpContentTypes.JsonWithCharSet;

        static JsonSerializerOptions GetJsonOptionsFromRequest(HttpContext httpContext)
        {
            var defaultOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ??
                   defaultOptions;
        }
    }

    private static async Task<string> ConvertToXmlAsync(object value, EndpointFilterInvocationContext context,
        Stream content)
    {
        await Task.CompletedTask;
        var resultType = value.GetType();
        var serializer = new XmlSerializer(resultType);
        serializer.Serialize(content, value);

        return HttpContentTypes.Xml;
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