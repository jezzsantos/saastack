using System.Net;
using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Colors = Infrastructure.Common.ConsoleConstants.Colors;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a request and response filter that records the request and response outputs.
///     Note: In TESTINGONLY the trace includes console colors to stand out in local development in the console.
///     There is no trace coloring in production.
///     Note: Since this is implemented as an <see cref="IEndpointFilter" /> we are able to determine
///     the type of response coming from the endpoint, and thus extract the RFC7807 contents easily here.
///     This would not be straight forward (if at all possible without dealing with response streams),
///     if we implemented this as ASPNET middleware.
/// </summary>
public class HttpRecordingFilter : IEndpointFilter
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IRecorder _recorder;

    public HttpRecordingFilter(IRecorder recorder, ICallerContextFactory callerContextFactory)
    {
        _recorder = recorder;
        _callerContextFactory = callerContextFactory;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var caller = _callerContextFactory.Create();
        var httpRequest = context.HttpContext.Request;
        var requestDescriptor = httpRequest.ToDisplayName();

        TraceRequest(_recorder, caller, requestDescriptor);

        var response = await next(context); //Continue down the pipeline
        if (response is null)
        {
            return response;
        }

        TraceResponse(_recorder, caller, context, requestDescriptor, response);
        return response;
    }

    private static void TraceRequest(IRecorder recorder, ICallerContext caller, string requestDescriptor)
    {
        recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
            $"{Colors.Blue}{{Request}}{Colors.Normal}: Received",
#else
            "{Request}: Received",
#endif
            requestDescriptor);
    }

    private static void TraceResponse(IRecorder recorder, ICallerContext caller,
        EndpointFilterInvocationContext context, string requestDescriptor, object response)
    {
        var httpResponse = context.HttpContext.Response;
        var statusCode = httpResponse.StatusCode;

        if (response is IStatusCodeHttpResult statusCodeResult)
        {
            statusCode = statusCodeResult.StatusCode ?? statusCode;
        }

        // Log the outgoing response
        if (response is IValueHttpResult { Value: ProblemDetails problemDetails })
        {
            statusCode = problemDetails.Status ?? statusCode;
            var responseBody = problemDetails.ToJson()!;
            RecordErrors(responseBody);
            return;
        }

        RecordSuccess();
        return;

        void RecordSuccess()
        {
            var statusCodeDescription = $"{statusCode} - {(HttpStatusCode)statusCode}";
            recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
                $"{Colors.Green}{{Request}}: {{Result}}{Colors.Normal}",
#else
                "{Request}: {Result}",
#endif
                requestDescriptor, statusCodeDescription);
        }

        void RecordErrors(string errorDetails)
        {
            var statusCodeDescription = $"{statusCode} - {(HttpStatusCode)statusCode}";
            switch (statusCode)
            {
                case >= 500:
                    recorder.TraceError(caller.ToCall(),
#if TESTINGONLY
                        $"{Colors.Red}{Colors.Bold}{{Request}}{Colors.NoBold}: {{Result}}{Colors.Normal}, problem: {{Problem}}",
#else
                        "{Request}: {Result}, problem: {Problem}",
#endif
                        requestDescriptor, statusCodeDescription, errorDetails);
                    break;

                case >= 400:
                    recorder.TraceInformation(caller.ToCall(),
#if TESTINGONLY
                        $"{Colors.Yellow}{{Request}}: {{Result}}{Colors.Normal}, problem: {{Problem}}",
#else
                        "{Request}: {Result}, problem: {Problem}",
#endif
                        requestDescriptor, statusCodeDescription, errorDetails);
                    break;

                default:
                    RecordSuccess();
                    break;
            }
        }
    }
}