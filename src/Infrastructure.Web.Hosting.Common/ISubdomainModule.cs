using System.Reflection;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Defines a individually deployable subdomain module
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public partial interface ISubdomainModule
{
    /// <summary>
    ///     Returns the assembly containing the DDD domain types
    /// </summary>
    Assembly? DomainAssembly { get; }

    /// <summary>
    ///     Returns the naming prefix for each aggregate and each entity in the <see cref="DomainAssembly" />
    /// </summary>
    Dictionary<Type, string> EntityPrefixes { get; }

    /// <summary>
    ///     Returns the assembly containing the infrastructure and API definitions
    /// </summary>
    Assembly InfrastructureAssembly { get; }
}