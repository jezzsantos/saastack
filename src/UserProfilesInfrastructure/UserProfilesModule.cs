using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserProfilesApplication;
using UserProfilesApplication.Persistence;
using UserProfilesDomain;
using UserProfilesInfrastructure.Api;
using UserProfilesInfrastructure.ApplicationServices;
using UserProfilesInfrastructure.Persistence;
using UserProfilesInfrastructure.Persistence.ReadModels;

namespace UserProfilesInfrastructure;

public class UserProfilesModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(UserProfilesApi).Assembly;

    public Assembly DomainAssembly => typeof(UserProfileRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(UserProfileRoot), "profile" }
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
                services.AddSingleton<IUserProfilesApplication>(c =>
                    new UserProfilesApplication.UserProfilesApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<IUserProfileRepository>()));
                services.AddSingleton<IUserProfileRepository>(c => new UserProfileRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<UserProfileRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<UserProfileRoot, UserProfileProjection>(
                    c => new UserProfileProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));

                services.AddSingleton<IUserProfilesService, UserProfilesInProcessServiceClient>();
            };
        }
    }
}