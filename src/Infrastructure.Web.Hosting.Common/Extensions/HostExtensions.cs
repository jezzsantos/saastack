using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Configuration;
using Domain.Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Infrastructure.Common;
using Infrastructure.Common.DomainServices;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Infrastructure.Hosting.Common;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Hosting.Common.Recording;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if TESTINGONLY
using Infrastructure.Persistence.Interfaces.ApplicationServices;

#else
using Infrastructure.Persistence.Common.ApplicationServices;
#endif

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class HostExtensions
{
    /// <summary>
    ///     Configures a WebHost
    /// </summary>
    public static WebApplication ConfigureApiHost(this WebApplicationBuilder builder, SubDomainModules modules,
        WebHostOptions options)
    {
        ConfigureSharedServices();
        ConfigureRecording();
        ConfigureMultiTenancy(options.IsMultiTenanted);
        ConfigureConfiguration(options.IsMultiTenanted);
        ConfigureAuthenticationAuthorization();
        ConfigureWireFormats();
        ConfigureApiRequests();
        ConfigureApplicationServices();
        ConfigurePersistence(options.Persistence.UsesQueues);

        var app = builder.Build();

        app.EnableRequestRewind();
        app.AddExceptionShielding();
        //TODO: app.AddMultiTenancyDetection(); we need a TenantDetective
        app.AddEventingListeners(options.Persistence.UsesEventing);
        app.EnableApiUsageTracking(options.TrackApiUsage);
        //TODO: add the Healthcheck endpoint
        //TODO: enable CORS

        modules.ConfigureHost(app);

        return app;

        void ConfigureSharedServices()
        {
            builder.Services.AddHttpContextAccessor();
        }

        void ConfigureRecording()
        {
            builder.Services.RegisterUnshared<IRecorder>(c =>
                new HostRecorder(c.ResolveForUnshared<IDependencyContainer>(), c.ResolveForUnshared<ILoggerFactory>(),
                    options));
        }

        void ConfigureMultiTenancy(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                builder.Services.RegisterTenanted<ITenancyContext, SimpleTenancyContext>();
            }
        }

        void ConfigureConfiguration(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                builder.Services.RegisterUnshared<ITenantSettingsService, AspNetHostLocalFileTenantSettingsService>();
                builder.Services.RegisterUnshared<ITenantSettingService>(c => new TenantSettingService(
                    new AesEncryptionService(c
                        .ResolveForTenant<IConfigurationSettings>().Platform
                        .GetString(TenantSettingService.EncryptionServiceSecretSettingName))));
                builder.Services.RegisterTenanted<IConfigurationSettings>(c =>
                    new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>(),
                        c.ResolveForTenant<ITenancyContext>()));
            }
            else
            {
                builder.Services.RegisterUnshared<IConfigurationSettings>(c =>
                    new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            }

            builder.Services.RegisterUnshared<IHostSettings>(c =>
                new HostSettings(new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>())));
        }

        void ConfigureAuthenticationAuthorization()
        {
            //TODO: need to add authentication/authorization (https://www.youtube.com/watch?v=XKN0084p7WQ)
        }

        void ConfigureApiRequests()
        {
            builder.Services.RegisterUnshared<IHasSearchOptionsValidator, HasSearchOptionsValidator>();
            builder.Services.RegisterUnshared<IHasGetOptionsValidator, HasGetOptionsValidator>();
            builder.Services.RegisterValidators(modules.ApiAssemblies, out var validators);

            builder.Services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssemblies(modules.ApiAssemblies.ToArray())
                    .AddValidatorBehaviors(validators, modules.ApiAssemblies);
            });
            modules.RegisterServices(builder.Configuration, builder.Services);
        }

        void ConfigureWireFormats()
        {
            builder.Services.ConfigureHttpJsonOptions(opts =>
            {
                opts.SerializerOptions.PropertyNameCaseInsensitive = true;
                opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.SerializerOptions.WriteIndented = false;
                opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase,
                    false));
                opts.SerializerOptions.Converters.Add(new JsonDateTimeConverter(DateFormat.Iso8601));
            });

            builder.Services.ConfigureHttpXmlOptions(opts => { opts.SerializerOptions.WriteIndented = false; });
        }

        void ConfigureApplicationServices()
        {
            builder.Services.AddHttpClient();
            var prefixes = modules.AggregatePrefixes;
            prefixes.Add(typeof(Checkpoint), "check");
            builder.Services.RegisterUnshared<IIdentifierFactory>(_ => new HostIdentifierFactory(prefixes));
            builder.Services.RegisterTenanted<ICallerContext, AnonymousCallerContext>();
        }

        void ConfigurePersistence(bool usesQueues)
        {
            var domainAssemblies = modules.DomainAssemblies
                .Concat(new[] { typeof(DomainCommonMarker).Assembly })
                .ToArray();
            builder.Services.RegisterUnshared<IDependencyContainer>(c => new DotNetDependencyContainer(c));
            builder.Services.RegisterUnshared<IDomainFactory>(c => DomainFactory.CreateRegistered(
                c.ResolveForUnshared<IDependencyContainer>(), domainAssemblies));
            builder.Services.RegisterUnshared<IEventSourcedChangeEventMigrator, ChangeEventTypeMigrator>();

#if TESTINGONLY
            RegisterStoreForTestingOnly(builder, usesQueues);
#else
            //HACK: we need a reasonable value for production here like SQLServerDataStore
            builder.Services.RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ => NullStore.Instance);
            builder.Services.RegisterTenanted<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ => NullStore.Instance);
#endif
        }

#if TESTINGONLY
        static void RegisterStoreForTestingOnly(WebApplicationBuilder builder, bool usesQueues)
        {
            builder.Services
                .RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.ResolveForUnshared<IConfigurationSettings>().Platform,
                        c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                            .ToOptional()));
            //HACK: In TESTINGONLY there won't be any physical partitioning of data for different tenants,
            // even if the host is multi-tenanted. So we can register a singleton for this specific store,
            // as we only ever want to resolve one instance for this store for all its uses (tenanted or unshared, except for platform use)
            builder.Services
                .RegisterUnshared<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.ResolveForUnshared<IConfigurationSettings>().Platform,
                        c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                            .ToOptional()));
            if (usesQueues)
            {
                RegisterStubMessageQueueDrainingService(builder);
            }
        }

        static void RegisterStubMessageQueueDrainingService(WebApplicationBuilder builder)
        {
            builder.Services.RegisterUnshared<IMonitoredMessageQueues, MonitoredMessageQueues>();
            builder.Services.RegisterUnshared<IQueueStoreNotificationHandler, StubQueueStoreNotificationHandler>();
            var drainApiMappings = new Dictionary<string, IWebRequest>
            {
                { "audits", new DrainAllAuditsRequest() },
                { "usages", new DrainAllUsagesRequest() }
                //     { "emails", new DrainAllEmailsRequest() },
                //     { "events", new DrainAllEventsRequest() },
            };
            builder.Services.AddHostedService(services =>
                new StubQueueDrainingService(services.GetRequiredService<IHttpClientFactory>(),
                    services.ResolveForUnshared<IHostSettings>(),
                    services.GetRequiredService<ILogger<StubQueueDrainingService>>(),
                    services.ResolveForUnshared<IMonitoredMessageQueues>(), drainApiMappings));
        }
#endif
    }
}