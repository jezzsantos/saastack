using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
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
        var description = member.GetCustomAttribute<DescriptionAttribute>();
        if (description.Exists())
        {
            if (description.Description.HasValue())
            {
                schema.Description = description.Description;
            }
        }
    }

    public static void SetDescription(this OpenApiParameter parameter, ParameterInfo parameterInfo)
    {
        var description = parameterInfo.GetCustomAttribute<DescriptionAttribute>();
        if (description.Exists())
        {
            if (description.Description.HasValue())
            {
                parameter.Description = description.Description;
            }
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