using System.Reflection;
using Application.Services.Shared;
using Common;
using Domain.Interfaces;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionsApplication;
using SubscriptionsApplication.ApplicationServices;
using SubscriptionsApplication.Persistence;
using SubscriptionsDomain;
using SubscriptionsInfrastructure.Api.Subscriptions;
using SubscriptionsInfrastructure.Persistence;
using SubscriptionsInfrastructure.Persistence.Notifications;
using SubscriptionsInfrastructure.Persistence.ReadModels;

namespace SubscriptionsInfrastructure;

public class SubscriptionsModule : ISubdomainModule
{
    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Assembly DomainAssembly => typeof(SubscriptionRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(SubscriptionRoot), "billsub" }
    };

    public Assembly InfrastructureAssembly => typeof(SubscriptionsApi).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddSingleton<IBillingProvider, SimpleBillingProvider>();

                services.AddPerHttpRequest<IChargebeeApplication, ChargebeeApplication>();
                services
                    .AddPerHttpRequest<ISubscriptionsApplication, SubscriptionsApplication.SubscriptionsApplication>();
                services.AddPerHttpRequest<ISubscriptionRepository, SubscriptionRepository>();
                services
                    .AddPerHttpRequest<IDomainEventNotificationConsumer>(c =>
                        new OrganizationNotificationConsumer(c.GetRequiredService<ICallerContextFactory>(),
                            c.GetRequiredService<ISubscriptionsApplication>()));
                services.RegisterEventing<SubscriptionRoot, SubscriptionProjection, SubscriptionNotifier>(
                    c => new SubscriptionProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IDataStore>()),
                    _ => new SubscriptionNotifier()
                );

                services.AddPerHttpRequest<ISubscriptionsService>(c =>
                    new SubscriptionsInProcessServiceClient(c.LazyGetRequiredService<ISubscriptionsApplication>()));
            };
        }
    }
}