using System.Reflection;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Interfaces;
using EventNotificationsApplication;
using EventNotificationsApplication.Persistence;
using EventNotificationsInfrastructure.Api.DomainEvents;
using EventNotificationsInfrastructure.ApplicationServices;
using EventNotificationsInfrastructure.Persistence;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventNotificationsInfrastructure;

public class EventNotificationsModule : ISubdomainModule
{
    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> EntityPrefixes => new();

    public Assembly InfrastructureAssembly => typeof(DomainEventsApi).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddSingleton<IDomainEventingMessageBusTopic>(c =>
                    new DomainEventingMessageBusTopic(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IMessageBusTopicMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IMessageBusStore>()));
                services.AddSingleton<IDomainEventingSubscriberService>(c =>
                    new ApiHostDomainEventingSubscriberService(
                        c.GetRequiredService<IRecorder>(),
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredServiceForPlatform<IMessageBusStore>()));
                services
                    .AddPerHttpRequest<IDomainEventingConsumerService, ApiHostDomainEventingConsumerService>();
                services.AddPerHttpRequest<IDomainEventsApplication, DomainEventsApplication>();
                services.AddPerHttpRequest<IEventNotificationRepository>(c =>
                    new EventNotificationRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
            };
        }
    }
}