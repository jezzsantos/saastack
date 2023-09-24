using Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.WebApi.Common;

public static class ApplicationExtensions
{
    /// <summary>
    ///     Provides a custom error when an exception is bubbled up.
    ///     Shows the exception stack trace if in development mode
    /// </summary>
    public static IApplicationBuilder AddExceptionShielding(this WebApplication app)
    {
        return app.UseExceptionHandler(configure =>
            configure.Run(async context =>
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
                    Status = 500,
                    Instance = context.Request.GetDisplayUrl(),
                    Detail = exceptionMessage
                };
                if (exceptionStackTrace.HasValue())
                {
                    details.Extensions.Add("exception", exceptionStackTrace);
                }


                await Results.Problem(details).ExecuteAsync(context);
            }));
    }

    /// <summary>
    ///     Whether we are in either <see cref="Environments.Development" /> or CI
    /// </summary>
    public static bool IsTestingOnly(this IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        return hostEnvironment.IsEnvironment(Environments.Development)
               || hostEnvironment.IsEnvironment("CI");
    }
}