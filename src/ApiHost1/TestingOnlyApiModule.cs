#if TESTINGONLY
using System.Reflection;
using ApiHost1.Api.TestingOnly;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

public class TestingOnlyApiModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(TestingWebApi).Assembly;

    public Assembly DomainAssembly => typeof(TestingWebApi).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new();

    public Action<WebApplication> MinimalApiRegistrationFunction
    {
        get { return app => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServicesFunction
    {
        get { return (_, _) => { }; }
    }
}
#endif