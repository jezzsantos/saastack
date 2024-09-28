using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Provides a declarative way to define the base path of an API
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BaseApiFromAttribute : Attribute
{
    public BaseApiFromAttribute(
#if !NETSTANDARD2_0
        [StringSyntax("Route")]
#endif
        string basePath)
    {
        BasePath = basePath;
    }

    public string BasePath { get; }
}