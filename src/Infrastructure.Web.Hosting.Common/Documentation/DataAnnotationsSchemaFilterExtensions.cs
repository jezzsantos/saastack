using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

internal static class DataAnnotationsSchemaFilterExtensions
{
    /// <summary>
    ///     Collates the required properties of the request type, into the schema
    /// </summary>
    public static void CollateRequiredProperties(this OpenApiSchema schema, Type requestType)
    {
        var properties = requestType.GetProperties();
        foreach (var property in properties)
        {
            // we have to add all required properties to the request collection
            if (property.IsPropertyRequired())
            {
                var name = property.Name.ToCamelCase();
                var required = schema.Required ?? new HashSet<string>();
                // ReSharper disable once PossibleUnintendedLinearSearchInSet
                if (!required.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    required.Add(name);
                }
            }
        }
    }

    public static bool IsPropertyRequired(this PropertyInfo property)
    {
        if (property.HasAttribute<RequiredAttribute>())
        {
            return true;
        }

        var requestDto = property.DeclaringType;
        var routeAttribute = requestDto!.GetCustomAttribute<RouteAttribute>();
        if (routeAttribute.NotExists())
        {
            return false;
        }

        var name = property.Name.ToCamelCase();
        return IsInRoute(routeAttribute, name);
    }

    /// <summary>
    ///     Determines if the type is a request or response type, which are the only ones that are annotatable
    ///     with <see cref="System.ComponentModel.DataAnnotations" /> attributes
    /// </summary>
    public static bool IsRequestOrResponseType(this Type? parent)
    {
        return parent.Exists()
               && (parent.IsAssignableTo(typeof(IWebRequest))
                   || parent.IsAssignableTo(typeof(IWebResponse)));
    }

    /// <summary>
    ///     Removes any properties from the schema that are used in the path of the route template,
    ///     which will be passed as route parameters
    /// </summary>
    public static void RemoveRouteTemplateFields(this OpenApiSchema schema, Type requestType)
    {
        var routeAttribute = requestType.GetCustomAttribute<RouteAttribute>();
        if (routeAttribute.NotExists())
        {
            return;
        }

        var route = routeAttribute.RouteTemplate;
        if (route.HasNoValue())
        {
            return;
        }

        if (!routeAttribute.Method.CanHaveBody())
        {
            return;
        }

        var placeholders = requestType.GetRouteTemplatePlaceholders();
        foreach (var placeholder in placeholders)
        {
            var property = schema.Properties.FirstOrDefault(prop => prop.Key.EqualsIgnoreCase(placeholder.Key));
            if (property.Exists())
            {
                schema.Properties.Remove(property.Key);
            }
        }
    }

    public static void SetDescription(this OpenApiParameter parameter, ParameterInfo parameterInfo)
    {
        var descriptionAttribute = parameterInfo.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttribute.Exists())
        {
            if (descriptionAttribute.Description.HasValue())
            {
                parameter.Description = descriptionAttribute.Description;
            }
        }
    }

    public static void SetEnumValues(this OpenApiSchema schema, Type type)
    {
        schema.Enum.Clear();
        schema.Type = "string";
        schema.Format = null;
        var names = Enum.GetNames(type).ToList();
        foreach (var name in names)
        {
            var bestName = name;
            var memberInfo = type.GetMember(name).FirstOrDefault(m => m.DeclaringType == type);
            if (memberInfo.Exists())
            {
                var enumMemberAttribute = memberInfo.GetCustomAttribute<EnumMemberAttribute>();
                if (enumMemberAttribute.Exists())
                {
                    bestName = enumMemberAttribute.Value ?? name;
                }
            }

            schema.Enum.Add(new OpenApiString(bestName));
        }
    }

    /// <summary>
    ///     Sets the description of a property of a requestType
    /// </summary>
    public static void SetPropertyDescription(this OpenApiSchema schema, MemberInfo property)
    {
        var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttribute.Exists())
        {
            if (descriptionAttribute.Description.HasValue())
            {
                schema.Description = descriptionAttribute.Description;
            }
        }
    }

    public static void SetRequired(this OpenApiParameter parameter, ParameterInfo parameterInfo)
    {
        if (parameter.In == ParameterLocation.Path
            || parameterInfo.GetCustomAttribute<RequiredAttribute>().Exists())
        {
            parameter.Required = true;
        }
    }

    private static bool IsInRoute(RouteAttribute routeAttribute, string name)
    {
        var route = routeAttribute.RouteTemplate;
        if (route.HasNoValue())
        {
            return false;
        }

        return route.Contains($"{{{name}}}", StringComparison.InvariantCultureIgnoreCase);
    }
}