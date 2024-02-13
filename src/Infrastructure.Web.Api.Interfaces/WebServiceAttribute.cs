using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Provides a declarative way to define a web API service
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class WebServiceAttribute : Attribute
{
    public WebServiceAttribute(
#if !NETSTANDARD2_0
        [StringSyntax("Route")]
#endif
        string basePath)
    {
        BasePath = basePath;
    }

    public string BasePath { get; }
}