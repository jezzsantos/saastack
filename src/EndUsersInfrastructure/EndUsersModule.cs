using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Interfaces;
using EndUsersApplication;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using EndUsersInfrastructure.Api.EndUsers;
using EndUsersInfrastructure.ApplicationServices;
using EndUsersInfrastructure.Persistence;
using EndUsersInfrastructure.Persistence.ReadModels;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EndUsersInfrastructure;

public class EndUsersModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(EndUsersApi).Assembly;

    public Assembly DomainAssembly => typeof(EndUserRoot).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(EndUserRoot), "user" },
        { typeof(Membership), "mship" }
    };

    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddSingleton<IEndUsersApplication>(c =>
                    new EndUsersApplication.EndUsersApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<IOrganizationsService>(),
                        c.GetRequiredService<IEndUserRepository>()));
                services.AddSingleton<IEndUserRepository>(c => new EndUserRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<EndUserRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<EndUserRoot, EndUserProjection>(
                    c => new EndUserProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));

                services.AddSingleton<IEndUsersService, EndUsersInProcessServiceClient>();
            };
        }
    }
}