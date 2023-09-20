using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     An action result which formats the given object as XML.
///     Note: We expect this class to be deleted once AspNetCore has its own XmlHttpResult
///     This class was very closely based off the <see cref="JsonHttpResult{TValue}" />
/// </summary>
public sealed class XmlHttpResult<TValue> : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>,
    IContentTypeHttpResult
{
    internal XmlHttpResult(TValue? value, int? statusCode)
    {
        Value = value;
        ContentType = HttpContentTypes.XmlWithCharset;

        if (value is ProblemDetails problemDetails)
        {
            MicrosoftAspNetCoreExtensions.ProblemDetailsDefaults.Apply(problemDetails, statusCode);
            statusCode ??= problemDetails.Status;
        }

        StatusCode = statusCode;
    }

    public string? ContentType { get; }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Infrastructure.WebApi.Common.XmlHttpResult");

        if (StatusCode is { } statusCode)
        {
            MicrosoftAspNetCoreExtensions.Log.WritingResultAsStatusCode(logger, statusCode);
            httpContext.Response.StatusCode = statusCode;
        }

        return MicrosoftAspNetCoreExtensions.WriteResultAsXmlAsync(
            httpContext,
            logger,
            Value,
            ContentType);
    }

    public int? StatusCode { get; }

    object? IValueHttpResult.Value => Value;

    public TValue? Value { get; }
}

/// <summary>
///     This class contains copies of code found in the Microsoft.AspNetCore.Http types
///     that we need for creating our own XmlHttpResult class.
///     We expect this code to be deleted when Microsoft releases their own XmlHttpResult type
/// </summary>
internal static partial class MicrosoftAspNetCoreExtensions
{
    public static Task WriteResultAsXmlAsync<T>(
        HttpContext httpContext,
        ILogger logger,
        T? value,
        string? contentType = null)
    {
        if (value is null)
        {
            return Task.CompletedTask;
        }

        var declaredType = typeof(T);
        if (declaredType.IsValueType)
        {
            Log.WritingResultAsXml(logger, declaredType.Name);

            // In this case the polymorphism is not
            // relevant and we don't need to box.
            return httpContext.Response.WriteAsXmlAsync(
                value,
                contentType);
        }

        var runtimeType = value.GetType();

        Log.WritingResultAsXml(logger, runtimeType.Name);

        return httpContext.Response.WriteAsXmlAsync(
            value,
            runtimeType,
            contentType);
    }

    private static Task WriteAsXmlAsync<TValue>(
        this HttpResponse response,
        TValue value,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        response.ContentType = contentType ?? HttpContentTypes.XmlWithCharset;

        // if no user provided token, pass the RequestAborted token and ignore OperationCanceledException
        if (!cancellationToken.CanBeCanceled)
        {
            return WriteAsXmlAsyncSlow(response.Body, value, response.HttpContext.RequestAborted);
        }

        return XmlSerializer.SerializeAsync(response.Body, value, cancellationToken);
    }

    private static Task WriteAsXmlAsync(
        this HttpResponse response,
        object? value,
        Type type,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        response.ContentType = contentType ?? HttpContentTypes.XmlWithCharset;

        // if no user provided token, pass the RequestAborted token and ignore OperationCanceledException
        if (!cancellationToken.CanBeCanceled)
        {
            return WriteAsXmlAsyncSlow(response.Body, value, type, response.HttpContext.RequestAborted);
        }

        return XmlSerializer.SerializeAsync(response.Body, value, type, cancellationToken);
    }

    private static async Task WriteAsXmlAsyncSlow(
        Stream body,
        object? value,
        Type type,
        CancellationToken cancellationToken)
    {
        try
        {
            await XmlSerializer.SerializeAsync(body, value, type, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task WriteAsXmlAsyncSlow<TValue>(
        Stream body,
        TValue value,
        CancellationToken cancellationToken)
    {
        try
        {
            await XmlSerializer.SerializeAsync(body, value, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    ///     Copy of <see cref="ProblemDetailsDefaults" /> so that we can use
    ///     <see cref="ProblemDetailsDefaults.Apply" />
    /// </summary>
    internal static class ProblemDetailsDefaults
    {
        private static readonly Dictionary<int, (string Type, string Title)> Defaults = new()
        {
            [400] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request"
            ),

            [401] =
            (
                "https://tools.ietf.org/html/rfc7235#section-3.1",
                "Unauthorized"
            ),

            [403] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                "Forbidden"
            ),

            [404] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                "Not Found"
            ),

            [405] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.5.5",
                "Method Not Allowed"
            ),

            [406] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.5.6",
                "Not Acceptable"
            ),

            [409] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                "Conflict"
            ),

            [415] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.5.13",
                "Unsupported Media Type"
            ),

            [422] =
            (
                "https://tools.ietf.org/html/rfc4918#section-11.2",
                "Unprocessable Entity"
            ),

            [500] =
            (
                "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                "An error occurred while processing your request."
            )
        };

        public static void Apply(ProblemDetails problemDetails, int? statusCode)
        {
            // We allow StatusCode to be specified either on ProblemDetails or on the ObjectResult and use it to configure the other.
            // This lets users write <c>return Conflict(new Problem("some description"))</c>
            // or <c>return Problem("some-problem", 422)</c> and have the response have consistent fields.
            if (problemDetails.Status is null)
            {
                if (statusCode is not null)
                {
                    problemDetails.Status = statusCode;
                }
                else
                {
                    problemDetails.Status = problemDetails is HttpValidationProblemDetails
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status500InternalServerError;
                }
            }

            if (Defaults.TryGetValue(problemDetails.Status.Value, out var defaults))
            {
                problemDetails.Title ??= defaults.Title;
                problemDetails.Type ??= defaults.Type;
            }
        }
    }

    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information,
            "Setting HTTP status code {StatusCode}.",
            EventName = "WritingResultAsStatusCode")]
        public static partial void WritingResultAsStatusCode(ILogger logger, int statusCode);

        [LoggerMessage(3, LogLevel.Information, "Writing value of type '{Type}' as Xml.",
            EventName = "WritingResultAsXml")]
        public static partial void WritingResultAsXml(ILogger logger, string type);
    }

    internal static class XmlSerializer
    {
        public static async Task SerializeAsync(Stream responseBody, object? value, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (value is null)
            {
                return;
            }

            var resultType = value.GetType();
            await SerializeAsync(responseBody, value, resultType, cancellationToken);
        }

        public static async Task SerializeAsync(Stream responseBody, object? value, Type resultType,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            if (value is null)
            {
                return;
            }

            await using var stream = new FileBufferingWriteStream();
            var serializer = new System.Xml.Serialization.XmlSerializer(resultType);
            serializer.Serialize(stream, value);
            await stream.DrainBufferAsync(responseBody, cancellationToken);
        }
    }
}