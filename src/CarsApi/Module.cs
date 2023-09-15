using System.Reflection;
using Infrastructure.WebApi.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarsApi;

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

    public Assembly ApiAssembly => typeof(CarsApi).Assembly;
}