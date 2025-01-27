using System.Reflection;
using ApiHost1.Api.Health;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

/// <summary>
///     Provides a module for common services of an API host,
///     such as the <see cref="HealthApi" /> endpoints.
/// </summary>
public class ApiHostModule : ISubdomainModule
{
    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get
        {
            // The TestingOnlyApiModule is already used in TESTINGONLY, and it registers the endpoints of this module already.
            // We do not want to create duplicate endpoints here.
#if !TESTINGONLY
            return (app, _) => app.RegisterRoutes();
#else
            return (_, _) =>
            {
                // Add your host specific middleware here
            };
#endif
        }
    }

    public Assembly? DomainAssembly => null; // No domain assembly

    public Dictionary<Type, string> EntityPrefixes => new();

    public Assembly InfrastructureAssembly => typeof(HealthApi).Assembly;

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