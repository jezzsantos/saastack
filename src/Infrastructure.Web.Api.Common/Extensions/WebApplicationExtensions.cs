using System.Net;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Resources;
using Common;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class WebApplicationExtensions
{
    public static readonly Type[] IgnoredTrackedRequestTypes =
    {
        // Exclude these as they are not API's called by users
#if TESTINGONLY
        typeof(DrainAllAuditsRequest),
        typeof(DrainAllUsagesRequest),
        typeof(SearchAllAuditsRequest),
#endif
        //typeof(GetHealthCheckRequest),
        typeof(DeliverUsageRequest),
        typeof(DeliverAuditRequest),

        // Exclude these or we will get a Stackoverflow!
        typeof(RecordUseRequest),
        typeof(RecordMeasureRequest)
    };

    /// <summary>
    ///     Starts the relays for eventing projections and notifications
    /// </summary>
    public static IApplicationBuilder AddEventingListeners(this WebApplication app, bool usesEventing)
    {
        if (!usesEventing)
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            var readModelRelay = context.RequestServices.GetRequiredService<IEventNotifyingStoreProjectionRelay>();
            if (!readModelRelay.IsStarted)
            {
                readModelRelay.Start();
            }

            var notificationRelay = context.RequestServices.GetRequiredService<IEventNotifyingStoreNotificationRelay>();
            if (!notificationRelay.IsStarted)
            {
                notificationRelay.Start();
            }

            await next();
        });
    }

    /// <summary>
    ///     Provides a global handler when an exception is encountered, and converts the exception
    ///     to an <see href="https://datatracker.ietf.org/doc/html/rfc7807">RFC7807</see> error.
    ///     Note: Shows the exception stack trace if in development mode
    /// </summary>
    public static IApplicationBuilder AddExceptionShielding(this WebApplication app)
    {
        return app.UseExceptionHandler(configure => configure.Run(async context =>
        {
            var exceptionMessage = string.Empty;
            var exceptionStackTrace = string.Empty;
            if (app.Environment.IsTestingOnly())
            {
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature is not null)
                {
                    exceptionMessage = contextFeature.Error.Message;
                    exceptionStackTrace = contextFeature.Error.ToString();
                }
            }

            var details = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = Resources.WebApplicationExtensions_AddExceptionShielding_UnexpectedExceptionMessage,
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = context.Request.GetDisplayUrl(),
                Detail = exceptionMessage
            };
            if (exceptionStackTrace.HasValue())
            {
                details.Extensions.Add(HttpResponses.ProblemDetails.Extensions.ExceptionPropertyName,
                    exceptionStackTrace);
            }

            await Results.Problem(details)
                .ExecuteAsync(context);
        }));
    }

    /// <summary>
    ///     Enables the tracking of all inbound API calls
    /// </summary>
    /// <param name="app"></param>
    public static WebApplication EnableApiUsageTracking(this WebApplication app, bool tracksUsage)
    {
        if (!tracksUsage)
        {
            return app;
        }

        app.Use(async (context, next) =>
        {
            var recorder = context.RequestServices.GetRequiredService<IRecorder>();
            var caller = context.RequestServices.GetRequiredService<ICallerContext>();

            TrackUsage(context, recorder, caller);
            await next();
        });

        return app;
    }

    /// <summary>
    ///     Enables request buffering, so that request bodies can be read in filters
    /// </summary>
    public static void EnableRequestRewind(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        });
    }

    private static void TrackUsage(HttpContext httpContext, IRecorder recorder, ICallerContext caller)
    {
        var request = httpContext.ToWebRequest();
        var requestType = request?.GetType();
        if (requestType.NotExists())
        {
            return;
        }

        if (IgnoredTrackedRequestTypes.Contains(requestType))
        {
            return;
        }

        var requestName = requestType.Name.ToLowerInvariant();
        var additional = new Dictionary<string, object>
        {
            { UsageConstants.Properties.EndPoint, requestName }
        };
        var requestAsProperties = request
            .ToObjectDictionary()
            .ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
        if (requestAsProperties.TryGetValue(nameof(IIdentifiableResource.Id), out var id))
        {
            if (id.Exists())
            {
                additional.Add(nameof(IIdentifiableResource.Id), id);
            }
        }

        recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.Api.ApiEndpointRequested, additional);
    }
}