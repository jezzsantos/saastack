#if TESTINGONLY
using System.Reflection;
using ApiHost1.Api.TestingOnly;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

public class TestingOnlyApiModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(TestingWebApi).Assembly;

    public Assembly DomainAssembly => null!;

    public Dictionary<Type, string> AggregatePrefixes => new();

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get { return (_, _) => { }; }
    }

    public Action<WebApplication> ConfigureMiddleware
    {
        get { return app => app.RegisterRoutes(); }
    }
}
#endif