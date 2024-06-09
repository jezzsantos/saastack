using System.Net;
using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Eventing.Interfaces.Projections;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
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
    private const int CustomMiddlewareIndex = 300;

    /// <summary>
    ///     Provides request handling for a BEFFE
    /// </summary>
    public static void AddBEFFE(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, bool isBEFFE)
    {
        if (!isBEFFE)
        {
            return;
        }

        // Note: must be registered before CORS since it calls app.UsesRouting()
        middlewares.Add(new MiddlewareRegistration(30,
            app => { app.UsePathBase(new PathString(WebConstants.BackEndForFrontEndBasePath)); },
            "Pipeline: Website API is enabled: Route -> {Route}", WebConstants.BackEndForFrontEndBasePath));
        middlewares.Add(new MiddlewareRegistration(35, app =>
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
        }, "Pipeline: Serving static HTML/CSS/JS is enabled"));
        middlewares.Add(new MiddlewareRegistration(CustomMiddlewareIndex + 100, app =>
        {
            app.UseMiddleware<CSRFMiddleware>();
            app.UseMiddleware<ReverseProxyMiddleware>();
        }, "Pipeline: BEFFE reverse proxy with CSRF protection is enabled"));
    }

    /// <summary>
    ///     Provides a global handler when an exception is encountered, and converts the exception
    ///     to an <see href="https://datatracker.ietf.org/doc/html/rfc7807">RFC7807</see> error.
    ///     Note: Shows the exception stack trace if in development mode
    /// </summary>
    public static void AddExceptionShielding(this WebApplication builder,
        List<MiddlewareRegistration> middlewares)
    {
        middlewares.Add(new MiddlewareRegistration(20, app =>
        {
            app.UseExceptionHandler(configure => configure.Run(async context =>
            {
                var exceptionMessage = string.Empty;
                var exceptionStackTrace = string.Empty;
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (app.Environment.IsTestingOnly())
                {
                    if (contextFeature.Exists())
                    {
                        var exception = contextFeature.Error;
                        exceptionMessage = exception.Message;
                        exceptionStackTrace = exception.ToString();
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
                    details.Extensions.Add(HttpConstants.Responses.ProblemDetails.Extensions.ExceptionPropertyName,
                        exceptionStackTrace);
                }

                await Results.Problem(details)
                    .ExecuteAsync(context);
            }));
        }, "Pipeline: Exception Shielding is enabled"));
    }

    /// <summary>
    ///     Enables CORS for the host
    /// </summary>
    public static void EnableCORS(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, CORSOption cors)
    {
        if (cors == CORSOption.None)
        {
            return;
        }

        var httpContext = builder.Services.GetRequiredService<IHttpContextFactory>().Create(new FeatureCollection());
        var policy = builder.Services.GetRequiredService<ICorsPolicyProvider>()
            .GetPolicyAsync(httpContext, WebHostingConstants.DefaultCORSPolicyName).GetAwaiter().GetResult()!
            .ToString();

        middlewares.Add(new MiddlewareRegistration(40, app => { app.UseCors(); },
            "Pipeline: CORS is enabled: Policy -> {Policy}",
            policy));
    }

    /// <summary>
    ///     Starts the relays for eventing projections and eventing notifications
    /// </summary>
    public static void EnableEventingPropagation(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, bool usesEventing)
    {
        if (!usesEventing)
        {
            return;
        }

        middlewares.Add(new MiddlewareRegistration(CustomMiddlewareIndex + 40, app =>
        {
            app.Use(async (context, next) =>
            {
                var readModelRelay = context.RequestServices.GetRequiredService<IEventNotifyingStoreProjectionRelay>();
                readModelRelay.Start();

                var notificationRelay =
                    context.RequestServices.GetRequiredService<IEventNotifyingStoreNotificationRelay>();
                notificationRelay.Start();

                await next();

                readModelRelay.Stop();
                notificationRelay.Stop();
            });
        }, "Pipeline: Event Projections/Notifications are enabled"));

        var subscriber = builder.Services.GetService<IDomainEventingSubscriber>();
        if (subscriber.Exists())
        {
            var subscriptionName = subscriber.SubscriptionName;
            middlewares.Add(new MiddlewareRegistration(CustomMiddlewareIndex + 45,
                _ => { subscriber.Subscribe(CancellationToken.None).GetAwaiter().GetResult(); },
                $"Feature: Subscribed to {EventingConstants.Topics.DomainEvents} for -> {{Subscription}}",
                subscriptionName));
        }
    }

    /// <summary>
    ///     Enables tenant detection
    /// </summary>
    public static void EnableMultiTenancy(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, bool isEnabled)
    {
        if (!isEnabled)
        {
            return;
        }

        middlewares.Add(new MiddlewareRegistration(52, //Must be after authentication and before Authorization 
            app => { app.UseMiddleware<MultiTenancyMiddleware>(); },
            "Pipeline: Multi-Tenancy detection is enabled"));
    }

    /// <summary>
    ///     Enables other options
    /// </summary>
    public static void EnableOtherFeatures(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, WebHostOptions hostOptions)
    {
        middlewares.Add(new MiddlewareRegistration(-100, app => { app.UseMiddleware<ResultRecordingMiddleware>(); },
            "Feature: Result logging"));

        var loggers = builder.Services.GetServices<ILoggerProvider>()
            .Select(logger => logger.GetType().Name).Join(", ");
        middlewares.Add(new MiddlewareRegistration(-80, _ =>
        {
            //Nothing to register
        }, "Feature: Logging to -> {Providers}", loggers));

        var appSettings = ((ConfigurationManager)builder.Configuration).Sources
            .OfType<JsonConfigurationSource>()
            .Select(jsonSource => jsonSource.Path)
            .Join(", ");
        middlewares.Add(new MiddlewareRegistration(-70, _ =>
        {
            //Nothing to register
        }, "Feature: Configuration loaded from -> {Sources}", appSettings));

        var recorder = builder.Services.GetRequiredService<IRecorder>()
            .ToString()!;
        middlewares.Add(new MiddlewareRegistration(-60, _ =>
        {
            //Nothing to register
        }, "Feature: Recording with -> {Recorder}", recorder));

        if (hostOptions.UsesApiDocumentation)
        {
            var prefix = hostOptions.IsBackendForFrontEnd
                ? WebConstants.BackEndForFrontEndDocsPath.Trim('/')
                : string.Empty; //Note: puts the swagger docs at the root of the API
            var url = builder.Configuration.GetValue<string>(WebHostDefaults.ServerUrlsKey);
            var path = prefix.HasValue()
                ? $"{url}/{prefix}"
                : url!;
            middlewares.Add(new MiddlewareRegistration(-50,
                app =>
                {
                    app.MapSwagger();
                    app.UseSwaggerUI(options =>
                    {
                        var jsonEndpoint = WebConstants.SwaggerEndpointFormat.Format(hostOptions.HostVersion);
                        options.DocumentTitle = hostOptions.HostName;
                        options.SwaggerEndpoint(jsonEndpoint, hostOptions.HostName);
                        options.RoutePrefix = prefix;
                    });
                }, "Feature: Open API documentation enabled with Swagger UI -> {Path}", path));
        }

        var dataStore = builder.Services.GetRequiredServiceForPlatform<IDataStore>().GetType().Name;
        var eventStore = builder.Services.GetRequiredServiceForPlatform<IEventStore>().GetType().Name;
        var queueStore = builder.Services.GetRequiredServiceForPlatform<IQueueStore>().GetType().Name;
        var blobStore = builder.Services.GetRequiredServiceForPlatform<IBlobStore>().GetType().Name;
        var messageBusStore = builder.Services.GetRequiredServiceForPlatform<IMessageBusStore>().GetType().Name;
        middlewares.Add(new MiddlewareRegistration(-40, _ =>
            {
                //Nothing to register
            },
            "Feature: Platform Persistence stores: DataStore -> {DataStore}, EventStore -> {EventStore}, MessageBusStore -> {messageBusStore}, QueueStore -> {QueueStore}, BlobStore -> {BlobStore}",
            dataStore, eventStore, messageBusStore, queueStore, blobStore));

        middlewares.Add(new MiddlewareRegistration(56, app => { app.UseAntiforgery(); },
            "Pipeline: Anti-forgery detection"));
    }

    /// <summary>
    ///     Enables request buffering, so that request bodies can be read in filters.
    ///     Note: Required to read the request by <see cref="ContentNegotiationFilter" /> and by
    ///     <see cref="HttpRequestExtensions.VerifyHMACSignatureAsync" /> during HMAC signature verification, and by
    ///     <see cref="MultiTenancyMiddleware" />
    /// </summary>
    public static void EnableRequestRewind(this WebApplication builder,
        List<MiddlewareRegistration> middlewares)
    {
        middlewares.Add(new MiddlewareRegistration(10, app =>
        {
            app.Use(async (context, next) =>
            {
                context.Request.EnableBuffering();
                await next();
            });
        }, "Pipeline: Rewinding of requests is enabled"));
    }

    /// <summary>
    ///     Enables authentication and authorization
    /// </summary>
    public static void EnableSecureAccess(this WebApplication builder,
        List<MiddlewareRegistration> middlewares, AuthorizationOptions authorization)
    {
        if (authorization.HasNone)
        {
            return;
        }

        middlewares.Add(new MiddlewareRegistration(50, app => { app.UseAuthentication(); },
            "Pipeline: Authentication is enabled: HMAC -> {HMAC}, APIKeys -> {APIKeys}, Tokens -> {Tokens}",
            authorization.UsesHMAC, authorization.UsesApiKeys, authorization.UsesTokens));
        middlewares.Add(
            new MiddlewareRegistration(54, app => { app.UseAuthorization(); },
                "Pipeline: Authorization is enabled: Roles -> Enabled, Features -> Enabled"));
    }
}