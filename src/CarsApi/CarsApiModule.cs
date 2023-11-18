using System.Reflection;
using Application.Interfaces.Services;
using CarsApplication;
using CarsApplication.Persistence;
using CarsDomain;
using CarsInfrastructure.ApplicationServices;
using CarsInfrastructure.Persistence;
using CarsInfrastructure.Persistence.ReadModels;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarsApi;

public class CarsApiModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(Apis.Cars.CarsApi).Assembly;

    public Assembly DomainAssembly => typeof(CarRoot).Assembly;

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
                services.AddTenantedEventing<CarRoot, CarProjection>(
                    c => new CarProjection(c.GetRequiredService<IRecorder>(), c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IDataStore>())
                );

                services.AddScoped<ICarsService, CarsInProcessService>();
            };
        }
    }
}