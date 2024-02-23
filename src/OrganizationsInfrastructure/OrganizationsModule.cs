using System.Reflection;
using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Services;
using Infrastructure.Common.DomainServices;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrganizationsApplication;
using OrganizationsApplication.Persistence;
using OrganizationsDomain;
using OrganizationsInfrastructure.ApplicationServices;
using OrganizationsInfrastructure.Persistence;
using OrganizationsInfrastructure.Persistence.ReadModels;

namespace OrganizationsInfrastructure;

public class OrganizationsModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(OrganizationsModule).Assembly;

    public Assembly DomainAssembly => typeof(OrganizationRoot).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(OrganizationRoot), "org" }
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
                services.RegisterUnshared<ITenantSettingsService, AspNetHostLocalFileTenantSettingsService>();
                services.RegisterUnshared<ITenantSettingService>(c => new TenantSettingService(
                    new AesEncryptionService(c
                        .ResolveForPlatform<IConfigurationSettings>()
                        .GetString(TenantSettingService.EncryptionServiceSecretSettingName))));
                services.RegisterUnshared<IOrganizationsApplication>(c =>
                    new OrganizationsApplication.OrganizationsApplication(c.ResolveForUnshared<IRecorder>(),
                        c.ResolveForUnshared<IIdentifierFactory>(),
                        c.ResolveForUnshared<ITenantSettingsService>(),
                        c.ResolveForUnshared<ITenantSettingService>(),
                        c.ResolveForUnshared<IEndUsersService>(),
                        c.ResolveForUnshared<IOrganizationRepository>()));
                services.RegisterUnshared<IOrganizationRepository>(c => new OrganizationRepository(
                    c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(),
                    c.ResolveForUnshared<IEventSourcingDddCommandStore<OrganizationRoot>>(),
                    c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<OrganizationRoot, OrganizationProjection>(
                    c => new OrganizationProjection(c.ResolveForUnshared<IRecorder>(),
                        c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForPlatform<IDataStore>()));

                services.RegisterUnshared<IOrganizationsService>(c =>
                    new OrganizationsInProcessServiceClient(c.LazyResolveForUnshared<IOrganizationsApplication>()));
            };
        }
    }
}