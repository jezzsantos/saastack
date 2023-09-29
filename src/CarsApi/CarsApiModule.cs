using System.Reflection;
using CarsApplication;
using CarsApplication.Persistence;
using CarsDomain;
using CarsInfrastructure.Persistence;
using Infrastructure.WebApi.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarsApi;

public class CarsApiModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(Apis.Cars.CarsApi).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(CarRoot), "car" }
    };

    public Action<WebApplication> MinimalApiRegistrationFunction
    {
        get { return app => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServicesFunction
    {
        get
        {
            return (_, services) =>
            {
                services.AddScoped<ICarsApplication, CarsApplication.CarsApplication>();
                services.AddScoped<ICarRepository, CarRepository>();
            };
        }
    }
}