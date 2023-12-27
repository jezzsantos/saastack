using System.Net;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Resources;
using Common;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Web.Hosting.Common.Extensions;

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
    ///     Provides a global handler when an exception is encountered, and converts the exception
    ///     to an <see href="https://datatracker.ietf.org/doc/html/rfc7807">RFC7807</see> error.
    ///     Note: Shows the exception stack trace if in development mode
    /// </summary>
    public static IApplicationBuilder AddExceptionShielding(this WebApplication app)
    {
        app.Logger.LogInformation("Exception Shielding is enabled");
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
    public static IApplicationBuilder EnableApiUsageTracking(this WebApplication app, bool tracksUsage)
    {
        if (!tracksUsage)
        {
            return app;
        }

        app.Logger.LogInformation("API Usage Tracking is enabled");
        return app.Use(async (context, next) =>
        {
            var recorder = context.RequestServices.GetRequiredService<IRecorder>();
            var caller = context.RequestServices.GetRequiredService<ICallerContext>();

            TrackUsage(context, recorder, caller);
            await next();
        });
    }

    /// <summary>
    ///     Enables CORS for the host
    /// </summary>
    public static IApplicationBuilder EnableCORS(this WebApplication app, CORSOption cors)
    {
        if (cors == CORSOption.None)
        {
            return app;
        }

        var httpContext = app.Services.GetRequiredService<IHttpContextFactory>().Create(new FeatureCollection());
        var policy = app.Services.GetRequiredService<ICorsPolicyProvider>()
            .GetPolicyAsync(httpContext, WebHostingConstants.DefaultCORSPolicyName).GetAwaiter().GetResult();
        app.Logger.LogInformation("CORS is enabled: Policy -> {Policy}", policy!.ToString());
        return app.UseCors();
    }

    /// <summary>
    ///     Starts the relays for eventing projections and notifications
    /// </summary>
    public static IApplicationBuilder EnableEventingListeners(this WebApplication app, bool usesEventing)
    {
        if (!usesEventing)
        {
            return app;
        }

        app.Logger.LogInformation("Eventing Projections/Notifications is enabled");
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
    ///     Enables other options
    /// </summary>
    public static IApplicationBuilder EnableOtherOptions(this WebApplication app, WebHostOptions hostOptions)
    {
        var loggers = app.Services.GetServices<ILoggerProvider>()
            .Select(logger => logger.GetType().Name).Join(", ");
        app.Logger.LogInformation("Logging to -> {Providers}", loggers);

        var appSettings = ((ConfigurationManager)app.Configuration).Sources
            .OfType<JsonConfigurationSource>()
            .Select(jsonSource => jsonSource.Path)
            .Join(", ");
        app.Logger.LogInformation("Configuration loaded from -> {Sources}", appSettings);

        var recorder = app.Services.GetRequiredService<IRecorder>();
        app.Logger.LogInformation("Recording with -> {Recorder}", recorder.ToString());

        app.Logger.LogInformation("Multi-Tenancy request detection is {Status}", hostOptions.IsMultiTenanted
            ? "disabled"
            : "enabled");

        var dataStore = app.Services.ResolveForPlatform<IDataStore>().GetType().Name;
        var eventStore = app.Services.ResolveForPlatform<IEventStore>().GetType().Name;
        var queueStore = app.Services.ResolveForPlatform<IQueueStore>().GetType().Name;
        var blobStore = app.Services.ResolveForPlatform<IBlobStore>().GetType().Name;
        app.Logger.LogInformation(
            "Platform Persistence stores: DataStore -> {DataStore} EventStore -> {EventStore} QueueStore -> {QueueStore} BlobStore -> {BlobStore}",
            dataStore, eventStore, queueStore, blobStore);
#if TESTINGONLY
        var stubDrainingServices = app.Services.GetServices<IHostedService>()
            .OfType<StubQueueDrainingService>()
            .ToList();
        if (stubDrainingServices.HasAny())
        {
            var stubDrainingService = stubDrainingServices[0];
            var queues = stubDrainingService.MonitoredQueues.Join(", ");
            app.Logger.LogInformation("Background queue draining on queues -> {Queues}", queues);
        }
#endif

        return app;
    }

    /// <summary>
    ///     Enables request buffering, so that request bodies can be read in filters
    /// </summary>
    public static IApplicationBuilder EnableRequestRewind(this WebApplication app)
    {
        return app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        });
    }

    /// <summary>
    ///     Enables authentication and authorization
    /// </summary>
    public static IApplicationBuilder EnableSecureAccess(this WebApplication app, bool usesAuth)
    {
        if (!usesAuth)
        {
            return app;
        }

        app.Logger.LogInformation("Authentication is enabled");
        app.Logger.LogInformation("RBAC Authorization is enabled");
        return app.UseAuthentication()
            .UseAuthorization();
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