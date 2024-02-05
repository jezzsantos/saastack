using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
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
        { typeof(EndUserRoot), "user" }
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
                services.RegisterUnshared<IEndUsersApplication, EndUsersApplication.EndUsersApplication>();
                services.RegisterUnshared<IEndUserRepository>(c => new EndUserRepository(
                    c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(),
                    c.ResolveForUnshared<IEventSourcingDddCommandStore<EndUserRoot>>(),
                    c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<EndUserRoot, EndUserProjection>(
                    c => new EndUserProjection(c.ResolveForUnshared<IRecorder>(),
                        c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForPlatform<IDataStore>()));

                services.RegisterUnshared<IEndUsersService, EndUsersInProcessServiceClient>();
            };
        }
    }
}