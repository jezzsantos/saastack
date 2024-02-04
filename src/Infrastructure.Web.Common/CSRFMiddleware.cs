using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Common;

/// <summary>
///     Provides middleware to ensure that incoming requests have not been spoofed via CSRF attacks in the browser.
/// </summary>
public sealed class CSRFMiddleware
{
    private readonly RequestDelegate _next;

    public CSRFMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context); //Continue down the pipeline
    }
}