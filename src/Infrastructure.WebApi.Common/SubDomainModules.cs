using System.Reflection;
using Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Modules used for registering subdomains
/// </summary>
public class SubDomainModules
{
    private readonly List<Assembly> _apiAssemblies = new();
    private readonly List<Action<WebApplication>> _minimalApiRegistrationFunctions = new();
    private readonly List<Action<ConfigurationManager, IServiceCollection>> _serviceCollectionFunctions = new();
    private readonly Dictionary<Type, string> _aggregatePrefixes = new();

    public IReadOnlyList<Assembly> ApiAssemblies => _apiAssemblies;

    public IDictionary<Type, string> AggregatePrefixes => _aggregatePrefixes;

    public void Register(ISubDomainModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(module.ApiAssembly, nameof(module.ApiAssembly));
        ArgumentNullException.ThrowIfNull(module.ApiAssembly, nameof(module.AggregatePrefixes));
        ArgumentNullException.ThrowIfNull(module.MinimalApiRegistrationFunction,
            nameof(module.MinimalApiRegistrationFunction));

        _apiAssemblies.Add(module.ApiAssembly);
        _aggregatePrefixes.Merge(module.AggregatePrefixes);
        _minimalApiRegistrationFunctions.Add(module.MinimalApiRegistrationFunction);
        if (module.RegisterServicesFunction is not null)
        {
            _serviceCollectionFunctions.Add(module.RegisterServicesFunction);
        }
    }

    public void RegisterServices(ConfigurationManager configuration, IServiceCollection serviceCollection)
    {
        _serviceCollectionFunctions.ForEach(func => func(configuration, serviceCollection));
    }

    public void ConfigureHost(WebApplication app)
    {
        _minimalApiRegistrationFunctions.ForEach(func => func(app));
    }
}