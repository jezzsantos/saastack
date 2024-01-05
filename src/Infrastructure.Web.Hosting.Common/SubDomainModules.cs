using System.Reflection;
using Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Modules used for registering subdomains
/// </summary>
public class SubDomainModules
{
    private readonly Dictionary<Type, string> _aggregatePrefixes = new();
    private readonly List<Assembly> _apiAssemblies = new();
    private readonly List<Assembly> _domainAssemblies = new();
    private readonly List<Action<WebApplication>> _minimalApiRegistrationFunctions = new();
    private readonly List<Action<ConfigurationManager, IServiceCollection>> _serviceCollectionFunctions = new();

    public IDictionary<Type, string> AggregatePrefixes => _aggregatePrefixes;

    public IReadOnlyList<Assembly> ApiAssemblies => _apiAssemblies;

    public IReadOnlyList<Assembly> DomainAssemblies => _domainAssemblies;

    public void ConfigureHost(WebApplication app)
    {
        _minimalApiRegistrationFunctions.ForEach(func => func(app));
    }

    public void Register(ISubDomainModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(module.ApiAssembly, nameof(module.ApiAssembly));
        ArgumentNullException.ThrowIfNull(module.ApiAssembly, nameof(module.AggregatePrefixes));
        ArgumentNullException.ThrowIfNull(module.ConfigureMiddleware,
            nameof(module.ConfigureMiddleware));

        _apiAssemblies.Add(module.ApiAssembly);
        if (module.DomainAssembly.Exists())
        {
            _domainAssemblies.Add(module.DomainAssembly);
        }

        _aggregatePrefixes.Merge(module.AggregatePrefixes);
        _minimalApiRegistrationFunctions.Add(module.ConfigureMiddleware);
        if (module.RegisterServices is not null)
        {
            _serviceCollectionFunctions.Add(module.RegisterServices);
        }
    }

    public void RegisterServices(ConfigurationManager configuration, IServiceCollection serviceCollection)
    {
        _serviceCollectionFunctions.ForEach(func => func(configuration, serviceCollection));
    }
}