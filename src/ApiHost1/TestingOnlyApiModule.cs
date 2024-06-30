#if TESTINGONLY
using System.Reflection;
using ApiHost1.Api.TestingOnly;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

public class TestingOnlyApiModule : ISubdomainModule
{
    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> EntityPrefixes => new();

    public Assembly InfrastructureAssembly => typeof(TestingWebApi).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get { return (_, _) => { }; }
    }
}
#endif