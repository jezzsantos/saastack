using System.Reflection;
using Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Modules used for registering subdomains
/// </summary>
public class SubdomainModules
{
    private readonly List<Assembly> _apiAssemblies = new();
    private readonly Dictionary<Type, string> _entityPrefixes = new();
    private readonly List<Action<WebApplication, List<MiddlewareRegistration>>>
        _minimalApiRegistrationFunctions = new();
    private readonly List<Action<ConfigurationManager, IServiceCollection>> _serviceCollectionFunctions = new();
    private readonly List<Assembly> _subdomainAssemblies = new();

    public IReadOnlyList<Assembly> ApiAssemblies => _apiAssemblies;

    public IDictionary<Type, string> EntityPrefixes => _entityPrefixes;

    public IReadOnlyList<Assembly> SubdomainAssemblies => _subdomainAssemblies;

    /// <summary>
    ///     Configure middleware in the pipeline
    /// </summary>
    public void ConfigureMiddleware(WebApplication app, List<MiddlewareRegistration> middlewares)
    {
        _minimalApiRegistrationFunctions.ForEach(func => func(app, middlewares));
    }

    /// <summary>
    ///     Registers all the information from the <see cref="ISubdomainModule" />
    /// </summary>
    public void Register(ISubdomainModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(module.InfrastructureAssembly, nameof(module.InfrastructureAssembly));
        ArgumentNullException.ThrowIfNull(module.InfrastructureAssembly, nameof(module.EntityPrefixes));
        ArgumentNullException.ThrowIfNull(module.ConfigureMiddleware,
            nameof(module.ConfigureMiddleware));

        _apiAssemblies.Add(module.InfrastructureAssembly);
        if (module.DomainAssembly.Exists())
        {
            _subdomainAssemblies.Add(module.DomainAssembly);
        }

        _entityPrefixes.Merge(module.EntityPrefixes);
        _minimalApiRegistrationFunctions.Add(module.ConfigureMiddleware);
        if (module.RegisterServices is not null)
        {
            _serviceCollectionFunctions.Add(module.RegisterServices);
        }
    }

    /// <summary>
    ///     Registers all the services with the dependency injection container
    /// </summary>
    public void RegisterServices(ConfigurationManager configuration, IServiceCollection serviceCollection)
    {
        _serviceCollectionFunctions.ForEach(func => func(configuration, serviceCollection));
    }
}