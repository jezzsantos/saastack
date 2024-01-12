using System.ComponentModel;
#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Provides a declarative way to define a REST route service operation, and configuration
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RouteAttribute : Attribute
{
    public RouteAttribute(
#if !NETSTANDARD2_0
        [StringSyntax("Route")]
#endif
        string routeTemplate, ServiceOperation operation,
        AccessType access = AccessType.Anonymous, bool isTestingOnly = false)
    {
        if (!Enum.IsDefined(typeof(ServiceOperation), operation))
        {
            throw new InvalidEnumArgumentException(nameof(operation), (int)operation, typeof(ServiceOperation));
        }

        RouteTemplate = routeTemplate;
        Operation = operation;
        Access = access;
        IsTestingOnly = isTestingOnly;
    }

    public AccessType Access { get; }

    public bool IsTestingOnly { get; }

    public ServiceOperation Operation { get; }

    public string RouteTemplate { get; }
}

/// <summary>
///     Defines the access level of the operation
/// </summary>
public enum AccessType
{
    Anonymous = 0,
    Token = 1,
    HMAC = 2
}