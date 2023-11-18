using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Defines a sub domain module
/// </summary>
public interface ISubDomainModule
{
    /// <summary>
    ///     Returns the naming prefix for each aggregate
    /// </summary>
    Dictionary<Type, string> AggregatePrefixes { get; }

    /// <summary>
    ///     Returns the assembly containing the API definition
    /// </summary>
    Assembly ApiAssembly { get; }

    /// <summary>
    ///     Returns the assembly containing the DDD domain types
    /// </summary>
    Assembly DomainAssembly { get; }

    /// <summary>
    ///     Returns a function that handles the minimal API registration
    /// </summary>
    Action<WebApplication> MinimalApiRegistrationFunction { get; }

    /// <summary>
    ///     Returns a function for using to register additional dependencies for this module
    /// </summary>
    Action<ConfigurationManager, IServiceCollection>? RegisterServicesFunction { get; }
}