using System.Net;
using Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.WebApi.Common;

public static class WebApplicationExtensions
{
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
                Title = "An unexpected error occurred",
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = context.Request.GetDisplayUrl(),
                Detail = exceptionMessage
            };
            if (exceptionStackTrace.HasValue())
            {
                details.Extensions.Add("exception", exceptionStackTrace);
            }

            await Results.Problem(details)
                .ExecuteAsync(context);
        }));
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
}