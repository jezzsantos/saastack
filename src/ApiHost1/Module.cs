using System.Reflection;
using Infrastructure.WebApi.Common;

namespace ApiHost1;

public class Module : ISubDomainModule
{
    public Action<WebApplication> MinimalApiRegistrationFunction
    {
        get { return app => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServicesFunction
    {
        get { return (configuration, services) => { }; }
    }

    public Assembly ApiAssembly => typeof(Program).Assembly;
}