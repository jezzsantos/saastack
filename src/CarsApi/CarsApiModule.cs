using System.Reflection;
using Application.Interfaces.Services;
using CarsApplication;
using CarsApplication.Persistence;
using CarsDomain;
using CarsInfrastructure.ApplicationServices;
using CarsInfrastructure.Persistence;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarsApi;

public class CarsApiModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(Apis.Cars.CarsApi).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(CarRoot), "car" },
        { typeof(UnavailabilityEntity), "unavail" }
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

                services.AddScoped<ICarsService, CarsInProcessService>();
            };
        }
    }
}