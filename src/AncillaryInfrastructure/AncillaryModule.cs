using System.Reflection;
using AncillaryApplication;
using AncillaryApplication.Persistence;
using AncillaryDomain;
using AncillaryInfrastructure.Api.Usages;
using AncillaryInfrastructure.ApplicationServices;
using AncillaryInfrastructure.Persistence;
using AncillaryInfrastructure.Persistence.ReadModels;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AncillaryInfrastructure;

public class AncillaryModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(UsagesApi).Assembly;

    public Assembly DomainAssembly => typeof(AuditRoot).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(AuditRoot), "audit" }
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
                services.RegisterUnshared<IAncillaryApplication, AncillaryApplication.AncillaryApplication>();
                services.RegisterUnshared<IUsageMessageQueueRepository>(c =>
                    new UsageMessageQueueRepository(c.Resolve<IRecorder>(), c.ResolveForPlatform<IQueueStore>()));
                services.RegisterUnshared<IAuditMessageQueueRepository>(c =>
                    new AuditMessageQueueRepository(c.Resolve<IRecorder>(), c.ResolveForPlatform<IQueueStore>()));
                services.RegisterUnshared<IAuditRepository>(c => new AuditRepository(c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(),
                    c.ResolveForUnshared<IEventSourcingDddCommandStore<AuditRoot>>(),
                    c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<AuditRoot, AuditProjection>(
                    c => new AuditProjection(c.ResolveForUnshared<IRecorder>(), c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForPlatform<IDataStore>()));

                services.RegisterUnshared<IUsageReportingService, NullUsageReportingService>();
            };
        }
    }
}