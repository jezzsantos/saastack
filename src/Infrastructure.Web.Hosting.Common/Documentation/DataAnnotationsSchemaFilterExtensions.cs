using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

internal static class DataAnnotationsSchemaFilterExtensions
{
    public static bool IsAnnotatable(this Type? parent)
    {
        return parent.Exists()
               && (parent.IsAssignableTo(typeof(IWebRequest))
                   || parent.IsAssignableTo(typeof(IWebResponse)));
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

    public static void SetDescription(this OpenApiSchema schema, MemberInfo member)
    {
        var descriptionAttribute = member.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttribute.Exists())
        {
            if (descriptionAttribute.Description.HasValue())
            {
                schema.Description = descriptionAttribute.Description;
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