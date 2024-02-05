using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Common;

/// <summary>
///     Provides middleware to detect the tenant of incoming requests
/// </summary>
public class MultiTenancyMiddleware
{
    private readonly RequestDelegate _next;

    public MultiTenancyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        //TODO: We need a TenantDetective that extracts the TenantId from the request,
        // or from the token being used 

        await _next(context); //Continue down the pipeline
    }
}