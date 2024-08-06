using System.Reflection;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that adds parameters for each POST, PUT, PATCH operation,
///     based on the route template defined in the <see cref="RouteAttribute" />.
/// </summary>
[UsedImplicitly]
public sealed class RouteTemplateParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var type = GetRequestType(context);
        if (!type.HasValue)
        {
            return;
        }

        var requestType = type.Value;
        operation.Parameters = AddRouteTemplateParameters(requestType, operation.Parameters);
    }

    private static IList<OpenApiParameter> AddRouteTemplateParameters(Type requestType,
        IList<OpenApiParameter> parameters)
    {
        var routeAttribute = requestType.GetCustomAttribute<RouteAttribute>();
        if (routeAttribute.NotExists())
        {
            return parameters;
        }

        var operation = routeAttribute.Method;
        if (!operation.CanHaveBody())
        {
            return parameters;
        }

        var placeholders = requestType.GetRouteTemplatePlaceholders();
        foreach (var placeholder in placeholders)
        {
            if (parameters.All(param => param.Name.NotEqualsIgnoreCase(placeholder.Key)))
            {
                parameters.Add(new OpenApiParameter
                {
                    Name = placeholder.Key,
                    In = ParameterLocation.Path,
                    Required = true,
                    Style = ParameterStyle.Simple,
                    Schema = new OpenApiSchema
                    {
                        Type = ToSchemaType(placeholder.Value)
                    }
                });
            }
        }

        return parameters;
    }

    private static string ToSchemaType(Type type)
    {
        if (type == typeof(string) || type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(double) || type == typeof(double?))
        {
            return "number";
        }

        if (type == typeof(int) || type == typeof(int?))
        {
            return "integer";
        }

        if (type == typeof(long) || type == typeof(long?))
        {
            return "integer";
        }

        if (type == typeof(bool) || type == typeof(bool?))
        {
            return "boolean";
        }

        return "string";
    }

    private static Optional<Type> GetRequestType(OperationFilterContext context)
    {
        var requestParameters = context.MethodInfo.GetParameters()
            .Where(IsWebRequest)
            .ToList();
        if (requestParameters.HasNone())
        {
            return Optional<Type>.None;
        }

        return requestParameters.First().ParameterType;

        static bool IsWebRequest(ParameterInfo requestParameter)
        {
            var type = requestParameter.ParameterType;
            if (type.NotExists())
            {
                return false;
            }

            return typeof(IWebRequest).IsAssignableFrom(type);
        }
    }
}