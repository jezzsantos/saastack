using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.WebApi.Common;

public static class HttpContextExtensions
{
    /// <summary>
    ///     Returns the default <see cref="IOptions{JsonOptions}" /> from the container
    /// </summary>
    public static JsonSerializerOptions ResolveJsonSerializerOptions(this HttpContext httpContext)
    {
        // Attempt to resolve options from DI then fallback to default options
        return httpContext.RequestServices.GetService<IOptions<JsonOptions>>()
            ?.Value.SerializerOptions ?? new JsonOptions().SerializerOptions;
    }

    /// <summary>
    ///     Returns the default <see cref="IOptions{XmlOptions}" /> from the container
    /// </summary>
    public static XmlSerializerOptions ResolveXmlSerializerOptions(this HttpContext httpContext)
    {
        // Attempt to resolve options from DI then fallback to default options
        return httpContext.RequestServices.GetService<IOptions<XmlOptions>>()
            ?.Value.SerializerOptions ?? XmlOptions.DefaultSerializerOptions;
    }
}