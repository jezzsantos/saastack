using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.WebApi.Interfaces;

/// <summary>
///     Provides a declarative way to define a REST route and service operation
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RouteAttribute : Attribute
{
    public RouteAttribute([StringSyntax("Route")] string routeTemplate, ServiceOperation operation,
        bool isTestingOnly = false)
    {
        if (!Enum.IsDefined(typeof(ServiceOperation), operation))
        {
            throw new InvalidEnumArgumentException(nameof(operation), (int)operation, typeof(ServiceOperation));
        }

        RouteTemplate = routeTemplate;
        Operation = operation;
        IsTestingOnly = isTestingOnly;
    }

    public bool IsTestingOnly { get; }

    public ServiceOperation Operation { get; }

    public string RouteTemplate { get; }
}