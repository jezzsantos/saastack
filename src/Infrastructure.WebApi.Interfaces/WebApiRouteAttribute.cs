using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.WebApi.Interfaces;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class WebApiRouteAttribute : Attribute
{
    public WebApiRouteAttribute([StringSyntax("Route")] string routeTemplate, WebApiOperation operation,
        bool isTestingOnly = false)
    {
        if (!Enum.IsDefined(typeof(WebApiOperation), operation))
        {
            throw new InvalidEnumArgumentException(nameof(operation), (int)operation, typeof(WebApiOperation));
        }

        RouteTemplate = routeTemplate;
        Operation = operation;
        IsTestingOnly = isTestingOnly;
    }

    public string RouteTemplate { get; }

    public WebApiOperation Operation { get; }

    public bool IsTestingOnly { get; }
}