using System.Net;
using Application.Common.Extensions;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Colors = Infrastructure.Common.ConsoleConstants.Colors;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

/// <summary>
///     Provides middleware to record the result of any request, whether successful or not
/// </summary>
public class ResultRecordingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRecorder _recorder;

    public ResultRecordingMiddleware(RequestDelegate next, IRecorder recorder)
    {
        _next = next;
        _recorder = recorder;
    }

    public async Task InvokeAsync(HttpContext context, ICallerContextFactory callerContextFactory)
    {
        var caller = callerContextFactory.Create();
        var httpRequest = context.Request;
        var requestDescriptor = httpRequest.ToDisplayName();

        // Log the incoming request
        _recorder.TraceInformation(caller.ToCall(), $"{Colors.Blue}{requestDescriptor}{Colors.Normal}: received");

        await _next(context); //Continue down the pipeline

        // Log the outgoing response
        var httpResponse = context.Response;

        var statusCodeDescription = $"{httpResponse.StatusCode} - {(HttpStatusCode)httpResponse.StatusCode}";
        if (httpResponse.StatusCode >= 500)
        {
            _recorder.TraceError(caller.ToCall(),
                $"{Colors.Red}{Colors.Bold}{requestDescriptor}{Colors.NoBold}: {statusCodeDescription}{Colors.Normal}");
            return;
        }

        var successResultColor = httpResponse.StatusCode >= 400
            ? Colors.Yellow
            : Colors.Green;
        _recorder.TraceInformation(caller.ToCall(),
            $"{successResultColor}{requestDescriptor}{Colors.Normal}: {statusCodeDescription}");
    }
}