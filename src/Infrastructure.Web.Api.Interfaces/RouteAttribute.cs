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

    /// <summary>
    ///     Access to the request.
    /// </summary>
    public AccessType Access { get; }

    /// <summary>
    ///     Whether this request is used only in testing, and not in production code
    /// </summary>
    public bool IsTestingOnly { get; }

    /// <summary>
    ///     The HTTP method used
    /// </summary>
    public OperationMethod Method { get; }

    /// <summary>
    ///     The route template. Supports substitutions from properties in the request class, in the format: {Property}
    ///     (case-insensitive)
    /// </summary>
#if !NETSTANDARD2_0
    [StringSyntax("Route")]
#endif
    public string RouteTemplate { get; }
}

/// <summary>
///     Defines the access level of the operation
/// </summary>
public enum AccessType
{
    /// <summary>
    ///     No authentication required
    /// </summary>
    Anonymous = 0,

    /// <summary>
    ///     Authenticated with Token authentication (e.g., a JWT Bearer token, in the HTTP Authorization header)
    /// </summary>
    Token = 1,

    /// <summary>
    ///     Authenticated with HMAC authentication, only used between hosts for "private" API access, for service account
    ///     machine-to-machine access
    /// </summary>
    HMAC = 2
}