using System.Reflection;
using ApiHost1.Api.Health;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

/// <summary>
///     Provides a module for common services of a API host
/// </summary>
public class ApiHostModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(HealthApi).Assembly;

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> EntityPrefixes => new();

    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get
        {
            return (_, _) =>
            {
                // Add you host specific middleware here
            };
        }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, _) =>
            {
                // Add your host specific dependencies here
            };
        }
    }
}