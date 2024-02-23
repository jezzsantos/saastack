using System.Diagnostics;
using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Resources;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using Infrastructure.Web.Api.Operations.Shared.Health;
using Microsoft.AspNetCore.Http;
using RecordMeasureRequest = Infrastructure.Web.Api.Operations.Shared.Ancillary.RecordMeasureRequest;
using RecordUseRequest = Infrastructure.Web.Api.Operations.Shared.Ancillary.RecordUseRequest;

namespace Infrastructure.Web.Api.Common.Endpoints;

/// <summary>
///     Provides a request filter that captures usage of the current request
/// </summary>
public class ApiUsageFilter : IEndpointFilter
{
    public static readonly Type[] IgnoredTrackedRequestTypes =
    {
        // Exclude these as they are not API's called by users
#if TESTINGONLY
        typeof(DrainAllAuditsRequest),
        typeof(DrainAllUsagesRequest),
        typeof(DrainAllEmailsRequest),
        typeof(SearchAllAuditsRequest),
#endif
        typeof(HealthCheckRequest),
        typeof(DeliverUsageRequest),
        typeof(DeliverAuditRequest),
        typeof(DeliverEmailRequest),

        // Exclude these or we will get a Stackoverflow!
        typeof(RecordUseRequest),
        typeof(RecordMeasureRequest),
        // Exclude these as they are not called by users
        typeof(Operations.Shared.BackEndForFrontEnd.RecordUseRequest),
        typeof(Operations.Shared.BackEndForFrontEnd.RecordMeasureRequest),
        typeof(RecordCrashRequest),
        typeof(RecordTraceRequest),
        typeof(RecordPageViewRequest)
    };
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IRecorder _recorder;

    public ApiUsageFilter(IRecorder recorder, ICallerContextFactory callerContextFactory)
    {
        _recorder = recorder;
        _callerContextFactory = callerContextFactory;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var caller = _callerContextFactory.Create();
        var stopwatch = Stopwatch.StartNew();
        TrackRequest(context, caller);

        var response = await next(context); //Continue down the pipeline

        stopwatch.Stop();
        TrackResponse(context, caller, stopwatch.Elapsed);

        return response;
    }

    private void TrackRequest(EndpointFilterInvocationContext context, ICallerContext caller)
    {
        var request = GetRequest(context);
        if (request.NotExists())
        {
            return;
        }

        var additional = PopulatePropertiesFromRequest(context.HttpContext, request, caller);

        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.Api.HttpRequestRequested, additional);
    }

    private void TrackResponse(EndpointFilterInvocationContext context, ICallerContext caller, TimeSpan duration)
    {
        var request = GetRequest(context);
        if (request.NotExists())
        {
            return;
        }

        var additional = PopulatePropertiesFromRequest(context.HttpContext, request, caller);
        var response = context.HttpContext.Response;
        var status = response.HasStarted
            ? context.HttpContext.Response.StatusCode
            : 0;
        additional.Add(UsageConstants.Properties.HttpStatusCode, status);
        additional.Add(UsageConstants.Properties.Duration, duration.TotalMilliseconds);

        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.Api.HttpRequestResponded, additional);
    }

    private static Dictionary<string, object> PopulatePropertiesFromRequest(HttpContext httpContext,
        IWebRequest request,
        ICallerContext caller)
    {
        var requestName = request.GetType().Name.ToLowerInvariant();
        var route = httpContext.GetEndpoint()!.DisplayName!;
        var httpRequest = httpContext.Request;
        var path = httpRequest.Path.Value!;
        var method = httpRequest.Method;
        var additional = new Dictionary<string, object>
        {
            { UsageConstants.Properties.EndPoint, requestName },
            { UsageConstants.Properties.UsedById, caller.CallerId },
            { UsageConstants.Properties.HttpRoute, route },
            { UsageConstants.Properties.HttpPath, path },
            { UsageConstants.Properties.HttpMethod, method }
        };
        var requestAsProperties = request
            .ToObjectDictionary()
            .ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
        if (requestAsProperties.TryGetValue(nameof(IIdentifiableResource.Id), out var id))
        {
            if (id.Exists())
            {
                additional.Add(nameof(UsageConstants.Properties.ResourceId), id);
            }
        }

        return additional;
    }

    private static IWebRequest? GetRequest(EndpointFilterInvocationContext context)
    {
        var arguments = context.Arguments;
        if (arguments.Count < 2)
        {
            return null;
        }

        var request = context.Arguments[1]!;
        if (request is not IWebRequest webRequest)
        {
            return null;
        }

        var requestType = webRequest.GetType();
        if (requestType.NotExists())
        {
            return null;
        }

        return IgnoredTrackedRequestTypes.Contains(requestType)
            ? null
            : webRequest;
    }
}