using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Services.Shared.DomainServices;
using EndUsersApplication;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using EndUsersInfrastructure.Api.EndUsers;
using EndUsersInfrastructure.ApplicationServices;
using EndUsersInfrastructure.Notifications;
using EndUsersInfrastructure.Persistence;
using EndUsersInfrastructure.Persistence.ReadModels;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EndUsersInfrastructure;

public class EndUsersModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(EndUsersApi).Assembly;

    public Assembly DomainAssembly => typeof(EndUserRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
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
                        c.GetRequiredService<INotificationsService>(),
                        c.GetRequiredService<IUserProfilesService>(),
                        c.GetRequiredService<IInvitationRepository>(),
                        c.GetRequiredService<IEndUserRepository>()));
                services.AddSingleton<IInvitationsApplication>(c =>
                    new InvitationsApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<ITokensService>(),
                        c.GetRequiredService<INotificationsService>(),
                        c.GetRequiredService<IUserProfilesService>(),
                        c.GetRequiredService<IInvitationRepository>()));
                services.AddSingleton<IEndUserRepository>(c => new EndUserRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<EndUserRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddSingleton<IInvitationRepository>(c => new InvitationRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<EndUserRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services
                    .AddPerHttpRequest<IDomainEventNotificationConsumer>(c =>
                        new EndUserDomainNotificationConsumer(c.GetRequiredService<ICallerContextFactory>(),
                            c.GetRequiredService<IEndUsersApplication>()));
                services.RegisterUnTenantedEventing<EndUserRoot, EndUserProjection, EndUserNotifier>(
                    c => new EndUserProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()),
                    c => new EndUserNotifier(c.GetRequiredService<IEnumerable<IDomainEventNotificationConsumer>>()));

                services.AddSingleton<IEndUsersService, EndUsersInProcessServiceClient>();
            };
        }
    }
}