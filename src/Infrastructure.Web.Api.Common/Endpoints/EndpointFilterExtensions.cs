using System.Reflection;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common.Endpoints;

public static class EndpointFilterExtensions
{
    private const int NumberOfParameters = 2;
    private const int RequestParameterIndex = 1;

    /// <summary>
    ///     Determines the request DTO of the current HTTP request.
    ///     As defined by the source generator for our minimal APIs,
    ///     we expect that all <see cref="RequestDelegate" /> are of this form: (IServiceProvider serviceProvider, IWebRequest
    ///     request) where the 2nd parameter must be an instance of a <see cref="IWebRequest{TResponse}" />
    /// </summary>
    public static IWebRequest? GetRequestDto(this EndpointFilterInvocationContext context)
    {
        var arguments = context.Arguments;
        if (arguments.Count < NumberOfParameters)
        {
            return null;
        }

        var request = context.Arguments[RequestParameterIndex]!;
        if (request is not IWebRequest webRequest)
        {
            return null;
        }

        return webRequest;
    }

    /// <summary>
    ///     Determines the request DTO of the current HTTP request.
    ///     As defined by the source generator for our minimal APIs,
    ///     we expect that all <see cref="RequestDelegate" /> are of this form: (IServiceProvider serviceProvider, IWebRequest
    ///     request) where the 2nd parameter must be an instance of a <see cref="IWebRequest{TResponse}" />
    /// </summary>
    public static Type? GetRequestDtoType(this HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint.NotExists())
        {
            return null;
        }

        var method = endpoint.Metadata.GetMetadata<MethodInfo>();
        if (method.NotExists())
        {
            return null;
        }

        var args = method.GetParameters();
        if (args.Length < NumberOfParameters)
        {
            return null;
        }

        var requestDtoType = args[RequestParameterIndex].ParameterType;
        if (!requestDtoType.IsAssignableTo(typeof(IWebRequest)))
        {
            return null;
        }

        return requestDtoType;
    }
}