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

public class OrganizationsModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(OrganizationsModule).Assembly;

    public Assembly DomainAssembly => typeof(OrganizationRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
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
                services.AddSingleton<ITenantSettingsService, AspNetHostLocalFileTenantSettingsService>();
                services.AddSingleton<ITenantSettingService>(c => new TenantSettingService(
                    new AesEncryptionService(c
                        .GetRequiredServiceForPlatform<IConfigurationSettings>()
                        .GetString(TenantSettingService.EncryptionServiceSecretSettingName))));
                services.AddSingleton<IOrganizationsApplication>(c =>
                    new OrganizationsApplication.OrganizationsApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<ITenantSettingsService>(),
                        c.GetRequiredService<ITenantSettingService>(),
                        c.GetRequiredService<IEndUsersService>(),
                        c.GetRequiredService<IOrganizationRepository>()));
                services.AddSingleton<IOrganizationRepository>(c => new OrganizationRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<OrganizationRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<OrganizationRoot, OrganizationProjection>(
                    c => new OrganizationProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));

                services.AddSingleton<IOrganizationsService>(c =>
                    new OrganizationsInProcessServiceClient(c.LazyGetRequiredService<IOrganizationsApplication>()));
            };
        }
    }
}