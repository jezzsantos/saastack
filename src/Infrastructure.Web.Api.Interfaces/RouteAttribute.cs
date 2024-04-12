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
        string routeTemplate, OperationMethod method,
        AccessType access = AccessType.Anonymous, bool isTestingOnly = false)
    {
        if (!Enum.IsDefined(typeof(OperationMethod), method))
        {
            throw new InvalidEnumArgumentException(nameof(method), (int)method, typeof(OperationMethod));
        }

        RouteTemplate = routeTemplate;
        Method = method;
        Access = access;
        IsTestingOnly = isTestingOnly;
    }

    public AccessType Access { get; }

    public bool IsTestingOnly { get; }

    public OperationMethod Method { get; }

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