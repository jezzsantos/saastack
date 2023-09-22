using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Defines a sub domain module
/// </summary>
public interface ISubDomainModule
{
    Assembly ApiAssembly { get; }

    Dictionary<Type, string> AggregatePrefixes { get; }

    Action<WebApplication> MinimalApiRegistrationFunction { get; }

    Action<ConfigurationManager, IServiceCollection>? RegisterServicesFunction { get; }
}