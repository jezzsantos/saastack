using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.WebApi.Common;

public class SubDomainModules
{
    private readonly List<Assembly> _handlerAssemblies;
    private readonly List<Action<WebApplication>> _minimalApiRegistrationFunctions;
    private readonly List<Action<ConfigurationManager, IServiceCollection>> _serviceCollectionFunctions;

    public SubDomainModules()
    {
        _handlerAssemblies = new List<Assembly>();
        _minimalApiRegistrationFunctions = new List<Action<WebApplication>>();
        _serviceCollectionFunctions = new List<Action<ConfigurationManager, IServiceCollection>>();
    }

    public IReadOnlyList<Assembly> HandlerAssemblies => _handlerAssemblies;

    public void Register(ISubDomainModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(module.ApiAssembly, nameof(module.ApiAssembly));
        ArgumentNullException.ThrowIfNull(module.MinimalApiRegistrationFunction,
            nameof(module.MinimalApiRegistrationFunction));

        _handlerAssemblies.Add(module.ApiAssembly);
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

public interface ISubDomainModule
{
    public Assembly ApiAssembly { get; }

    public Action<WebApplication> MinimalApiRegistrationFunction { get; }
    public Action<ConfigurationManager, IServiceCollection>? RegisterServicesFunction { get; }
}