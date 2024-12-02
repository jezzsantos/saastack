using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    ///     Collates the required properties of the request or response type, into the schema
    /// </summary>
    public static void CollateRequiredProperties(this OpenApiSchema schema, Type type)
    {
        var required = schema.Required ?? new HashSet<string>();
        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            if (IsRequestType(type))
            {
                if (property.IsRequestPropertyRequired())
                {
                    AddToRequired(property);
                }
                else
                {
                    RemoveFromRequired(property);
                }

                continue;
            }

            if (IsResponseType(type))
            {
                if (property.IsAnyPropertyRequired())
                {
                    AddToRequired(property);
                }
                else
                {
                    RemoveFromRequired(property);
                }

                continue;
            }

            // Either a request or repose DTO
            if (property.IsAnyPropertyRequired())
            {
                AddToRequired(property);
            }
            else
            {
                RemoveFromRequired(property);
            }
        }

        return;

        void AddToRequired(PropertyInfo property)
        {
            var name = property.Name.ToCamelCase();
            // ReSharper disable once PossibleUnintendedLinearSearchInSet
            if (!required.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                required.Add(name);
            }
        }

        void RemoveFromRequired(PropertyInfo property)
        {
            var name = property.Name.ToCamelCase();
            // ReSharper disable once PossibleUnintendedLinearSearchInSet
            if (required.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                required.Remove(name);
            }
        }
    }

    public static bool IsPropertyInRoute(this PropertyInfo property)
    {
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
        return IsRequestType(parent) || IsResponseType(parent);
    }

    public static bool IsRequestPropertyRequired(this PropertyInfo property)
    {
        if (property.HasAttribute<RequiredAttribute>())
        {
            return true;
        }

        return IsPropertyInRoute(property);
    }

    /// <summary>
    ///     Determines if the type is a request type, which are the only ones that are annotatable
    ///     with <see cref="System.ComponentModel.DataAnnotations" /> attributes
    /// </summary>
    public static bool IsRequestType(this Type? parent)
    {
        return parent.Exists() && parent.IsAssignableTo(typeof(IWebRequest));
    }

    /// <summary>
    ///     Determines if the type is a response type, which are the only ones that are annotatable
    ///     with <see cref="System.ComponentModel.DataAnnotations" /> attributes
    /// </summary>
    public static bool IsResponseType(this Type? parent)
    {
        return parent.Exists() && parent.IsAssignableTo(typeof(IWebResponse));
    }

    /// <summary>
    ///     Removes any properties from the schema that are used in the path of the route template,
    ///     which will be passed as route parameters
    /// </summary>
    public static void RemoveRouteTemplateFields(this OpenApiSchema schema, Type type)
    {
        var routeAttribute = type.GetCustomAttribute<RouteAttribute>();
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

        var placeholders = type.GetRouteTemplatePlaceholders();
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
        if (property.MemberType != MemberTypes.Property)
        {
            return;
        }

        var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttribute.Exists())
        {
            if (descriptionAttribute.Description.HasValue())
            {
                schema.Description = descriptionAttribute.Description;
            }
        }
    }

    /// <summary>
    ///     Sets the nullability of a property to false, so that it is never nullable
    ///     Note: nullable here (in OpenApi) means that the property would exist in the JSON with a value of "null"
    ///     We don't want "nullable", we will use "required" instead.
    /// </summary>
    public static void SetPropertyNullable(this OpenApiSchema schema, MemberInfo property)
    {
        if (property.MemberType != MemberTypes.Property)
        {
            return;
        }

        schema.Nullable = false;
    }

    public static void SetRequired(this OpenApiParameter parameter, ParameterInfo parameterInfo)
    {
        if (parameter.In == ParameterLocation.Path
            || parameterInfo.GetCustomAttribute<RequiredAttribute>().Exists())
        {
            parameter.Required = true;
        }
    }

    private static bool IsAnyPropertyRequired(this PropertyInfo propertyInfo)
    {
        return !propertyInfo.IsAnyPropertyNullable();
    }

    private static bool IsAnyPropertyNullable(this PropertyInfo propertyInfo)
    {
        var isNullable = false;

        // Check for the [Required] DataAnnotation attribute
        if (propertyInfo.GetCustomAttribute<RequiredAttribute>().Exists())
        {
            isNullable = false;
        }

        // Check for the 'required' C# keyword
        if (propertyInfo.GetCustomAttribute<RequiredMemberAttribute>().Exists())
        {
            isNullable = false;
        }

        if (propertyInfo.PropertyType.IsValueType)
        {
            isNullable = Nullable.GetUnderlyingType(propertyInfo.PropertyType).Exists();
        }

        // Check for the ? nullable annotation
        if (propertyInfo.GetCustomAttribute<NullableAttribute>().Exists())
        {
            isNullable = true;
        }

        return isNullable;
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