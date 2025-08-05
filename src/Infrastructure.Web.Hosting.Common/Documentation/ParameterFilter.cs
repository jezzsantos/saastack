using System.Reflection;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using RouteAttribute = Infrastructure.Web.Api.Interfaces.RouteAttribute;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that adds parameters for each request type.
///     For POST, PUT, PATCH operation:
///     1. Adds route parameters for each property defined in the <see cref="Api.Interfaces.RouteAttribute" />.
///     2. Adds query parameters for any property marked up with the [FromQuery] attribute.
///     For GET, DELETE operations:
///     1. Adds route parameters for each property defined in the <see cref="Api.Interfaces.RouteAttribute" />.
///     2. Adds query parameters for all other properties in the request type.
/// </summary>
[UsedImplicitly]
public sealed class ParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var type = context.GetRequestType();
        if (!type.HasValue)
        {
            return;
        }

        var requestType = type.Value;
        AddParameters(requestType, operation.Parameters);
    }

    private static void AddParameters(Type requestType, IList<OpenApiParameter> parameters)
    {
        var properties = requestType.GetProperties()
            .ToDictionary(prop => prop.Name, prop => prop);
        if (properties.HasNone())
        {
            return;
        }

        var routeAttribute = requestType.GetCustomAttribute<RouteAttribute>();
        if (routeAttribute.NotExists())
        {
            return;
        }

        var operation = routeAttribute.Method;
        var placeholders = requestType.GetRouteTemplatePlaceholders();
        foreach (var placeholder in placeholders)
        {
            var name = placeholder.Key;
            if (properties.TryGetValue(name, out var property))
            {
                AddRouteParameter(name, property);
                properties.Remove(name);
            }
        }

        foreach (var property in properties)
        {
            var name = property.Key;
            if (operation.CanHaveBody())
            {
                var isFromQuery = property.Value.HasAttribute<FromQueryAttribute>();
                if (isFromQuery)
                {
                    AddQueryParameter(name, property.Value);
                }
            }
            else
            {
                AddQueryParameter(name, property.Value);
            }
        }

        return;

        void AddRouteParameter(string name, PropertyInfo property)
        {
            var description = property.GetDescription();
            parameters.Add(new OpenApiParameter
            {
                Name = name,
                Description = description,
                In = ParameterLocation.Path,
                Required = true,
                Style = ParameterStyle.Simple,
                Schema = new OpenApiSchema
                {
                    Type = ToSchemaType(property.PropertyType)
                }
            });
        }

        void AddQueryParameter(string name, PropertyInfo property)
        {
            var isRequired = property.IsRequestPropertyRequired();
            var description = property.GetDescription();
            parameters.Add(new OpenApiParameter
            {
                Name = name,
                Description = description,
                In = ParameterLocation.Query,
                Required = isRequired,
                Style = ParameterStyle.Simple,
                Schema = new OpenApiSchema
                {
                    Type = ToSchemaType(property.PropertyType)
                }
            });
        }
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
}