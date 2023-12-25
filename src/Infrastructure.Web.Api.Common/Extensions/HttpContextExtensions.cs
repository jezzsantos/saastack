using System.Text.Json;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Web.Api.Common.Extensions;

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

    /// <summary>
    ///     Returns the current inbound request as a <see cref="IWebRequest" />
    /// </summary>
    public static IWebRequest? ToWebRequest(this HttpContext httpContext)
    {
        //HACK: We need to find a way to get to the type of our IWebRequest in the EndPoint.RequestDelegate
        //var request = httpContext.Request;
        // var endpoint = httpContext.GetEndpoint();
        // if (endpoint is null || endpoint.RequestDelegate is null)
        // {
        //     return null;
        // }

        //which should all be following the predictable pattern like this:
        // carsapiGroup.MapPatch("/cars/{id}/maintain",
        //     async (global::MediatR.IMediator mediator, global::Infrastructure.Web.Api.Operations.Shared.Cars.ScheduleMaintenanceCarRequest request) =>
        //         await mediator.Send(request, global::System.Threading.CancellationToken.None));
        // carsapiGroup.MapGet("/cars",
        //     async (global::MediatR.IMediator mediator, [global::Microsoft.AspNetCore.Http.AsParameters] global::Infrastructure.Web.Api.Operations.Shared.Cars.SearchAllCarsRequest request) =>
        //     await mediator.Send(request, global::System.Threading.CancellationToken.None));

        //We want the type of the XXXRequest class (2nd parameter of the handler function),
        // and then we want to return the actual instance of it

        return null;
    }
}