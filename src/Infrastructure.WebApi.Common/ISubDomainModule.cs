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
    Dictionary<Type, string> AggregatePrefixes { get; }

    Assembly ApiAssembly { get; }

    Action<WebApplication> MinimalApiRegistrationFunction { get; }

    Action<ConfigurationManager, IServiceCollection>? RegisterServicesFunction { get; }
}