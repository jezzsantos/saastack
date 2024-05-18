using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Interfaces;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserProfilesApplication;
using UserProfilesApplication.Persistence;
using UserProfilesDomain;
using UserProfilesInfrastructure.Api.Profiles;
using UserProfilesInfrastructure.ApplicationServices;
using UserProfilesInfrastructure.Notifications;
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
                services.AddSingleton<IAvatarService>(c =>
                    new GravatarHttpServiceClient(
                        c.GetRequiredService<IRecorder>(),
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<IHttpClientFactory>()));

                services.AddPerHttpRequest<IUserProfilesApplication>(c =>
                    new UserProfilesApplication.UserProfilesApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<IImagesService>(),
                        c.GetRequiredService<IAvatarService>(),
                        c.GetRequiredService<IUserProfileRepository>()));
                services.AddPerHttpRequest<IUserProfileRepository>(c =>
                    new UserProfileRepository(c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<UserProfileRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services
                    .AddPerHttpRequest<IDomainEventNotificationConsumer>(c =>
                        new EndUserNotificationConsumer(c.GetRequiredService<ICallerContextFactory>(),
                            c.GetRequiredService<IUserProfilesApplication>()));
                services
                    .AddPerHttpRequest<IDomainEventNotificationConsumer>(c =>
                        new ImageNotificationConsumer(c.GetRequiredService<ICallerContextFactory>(),
                            c.GetRequiredService<IUserProfilesApplication>()));
                services.RegisterEventing<UserProfileRoot, UserProfileProjection, UserProfileNotifier>(
                    c => new UserProfileProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()),
                    _ => new UserProfileNotifier());

                services.AddPerHttpRequest<IUserProfilesService, UserProfilesInProcessServiceClient>();
            };
        }
    }
}