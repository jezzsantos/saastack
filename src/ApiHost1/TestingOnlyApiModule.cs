using System.Reflection;
using Infrastructure.WebApi.Common;

namespace ApiHost1;

public class TestingOnlyApiModule : ISubDomainModule
{
    public Action<WebApplication> MinimalApiRegistrationFunction
    {
        get { return app => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServicesFunction
    {
        get { return (_, _) => { }; }
    }

    public Assembly ApiAssembly => typeof(Program).Assembly;
}