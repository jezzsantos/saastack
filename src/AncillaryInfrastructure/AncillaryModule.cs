using System.Reflection;
using AncillaryApplication;
using AncillaryApplication.Persistence;
using AncillaryDomain;
using AncillaryInfrastructure.Api.Usages;
using AncillaryInfrastructure.ApplicationServices;
using AncillaryInfrastructure.Persistence;
using AncillaryInfrastructure.Persistence.ReadModels;
using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Common;
using Domain.Interfaces;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AncillaryInfrastructure;

public class AncillaryModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(UsagesApi).Assembly;

    public Assembly DomainAssembly => typeof(AuditRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(AuditRoot), "audit" },
        { typeof(EmailDeliveryRoot), "emaildelivery" }
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
                services.AddSingleton<IRecordingApplication, RecordingApplication>();
                services.AddSingleton<IFeatureFlagsApplication, FeatureFlagsApplication>();
                services.AddSingleton<IAncillaryApplication, AncillaryApplication.AncillaryApplication>();
                services.AddSingleton<IUsageMessageQueue>(c =>
                    new UsageMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IMessageQueueIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<IAuditMessageQueueRepository>(c =>
                    new AuditMessageQueueRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IMessageQueueIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<IEmailMessageQueue>(c =>
                    new EmailMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IMessageQueueIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<IAuditRepository>(c => new AuditRepository(c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<AuditRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<AuditRoot, AuditProjection>(
                    c => new AuditProjection(c.GetRequiredService<IRecorder>(), c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddSingleton<IEmailDeliveryRepository>(c => new EmailDeliveryRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<EmailDeliveryRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<EmailDeliveryRoot, EmailDeliveryProjection>(
                    c => new EmailDeliveryProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddSingleton<IProvisioningMessageQueue>(c =>
                    new ProvisioningMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IMessageQueueIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));

                services.AddSingleton<IUsageDeliveryService, NoOpUsageDeliveryService>();
                services.AddSingleton<IEmailDeliveryService, NoOpEmailDeliveryService>();
                services.AddSingleton<IProvisioningNotificationService, OrganizationProvisioningNotificationService>();
            };
        }
    }
}