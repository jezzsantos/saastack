#if TESTINGONLY
using System.Reflection;
using ApiHost1.Apis.TestingOnly;
using CarsApi;
using Infrastructure.WebApi.Common;

namespace ApiHost1;

public class TestingOnlyApiModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(TestingWebApi).Assembly;

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