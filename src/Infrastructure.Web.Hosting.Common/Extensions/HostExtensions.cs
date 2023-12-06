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
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if TESTINGONLY
using Infrastructure.Persistence.Interfaces.ApplicationServices;
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
        ConfigurePersistence();

        var app = builder.Build();

        app.EnableRequestRewind();
        app.AddExceptionShielding();
        //TODO: app.AddMultiTenancyDetection(); we need a TenantDetective
        app.AddEventingListeners();

        modules.ConfigureHost(app);

        return app;

        void ConfigureSharedServices()
        {
            builder.Services.AddHttpContextAccessor();
        }

        void ConfigureRecording()
        {
            builder.Services.RegisterUnshared<IRecorder>(c =>
                new TracingOnlyRecorder(options.HostName,
                    c.ResolveForUnshared<ILoggerFactory>())); // TODO: we need a more comprehensive HostRecorder using Azure or AWS or GC
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

            builder.Services.RegisterUnshared<IApiHostSetting>(c =>
                new WebApiHostSettings(new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>())));
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
            builder.Services.RegisterUnshared<IIdentifierFactory>(_ =>
                new HostIdentifierFactory(modules.AggregatePrefixes));
            builder.Services.RegisterTenanted<ICallerContext, AnonymousCallerContext>();
        }

        void ConfigurePersistence()
        {
            var domainAssemblies = modules.DomainAssemblies
                .Concat(new[] { typeof(DomainCommonMarker).Assembly })
                .ToArray();
            builder.Services.RegisterUnshared<IDependencyContainer>(c => new DotNetDependencyContainer(c));
            builder.Services.RegisterUnshared<IDomainFactory>(c => DomainFactory.CreateRegistered(
                c.ResolveForUnshared<IDependencyContainer>(), domainAssemblies));
            builder.Services.RegisterUnshared<IEventSourcedChangeEventMigrator, ChangeEventTypeMigrator>();

#if TESTINGONLY
            builder.Services
                .RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, InProcessInMemStore>(StoreExtensions
                    .GetStoreForTestingOnly);
            builder.Services
                .RegisterTenanted<IDataStore, IEventStore, IBlobStore, IQueueStore, InProcessInMemStore>(StoreExtensions
                    .GetStoreForTestingOnly);
            RegisterStubMessageQueueDrainingService();
#else
            //HACK: we need a reasonable value for production here like SQLServerDataStore
            builder.Services.RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ => NullStore.Instance);
            builder.Services.RegisterTenanted<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ => NullStore.Instance);
#endif
        }

#if TESTINGONLY
        void RegisterStubMessageQueueDrainingService()
        {
            builder.Services.RegisterUnshared<IMonitoredMessageQueues, MonitoredMessageQueues>();
            builder.Services.RegisterUnshared<IQueueStoreNotificationHandler, StubQueueStoreNotificationHandler>();
            var drainApiMappings = new Dictionary<string, IWebRequest>();
            // TODO: add these requests for testing locally
            // {
            //     { "emails", new DrainAllEmailsRequest() },
            //     { "audits", new DrainAllAuditsRequest() },
            //     { "usages", new DrainAllUsagesRequest() },
            //     { "events", new DrainAllEventsRequest() },
            // };
            builder.Services.AddHostedService(services =>
                new StubQueueDrainingService(services.GetRequiredService<IHttpClientFactory>(),
                    services.ResolveForUnshared<IApiHostSetting>(),
                    services.GetRequiredService<ILogger<StubQueueDrainingService>>(),
                    services.ResolveForUnshared<IMonitoredMessageQueues>(), drainApiMappings));
        }
#endif
    }
}

#if TESTINGONLY
internal static class StoreExtensions
{
    private static InProcessInMemStore? _store;

    public static InProcessInMemStore GetStoreForTestingOnly(IServiceProvider container)
    {
        // HACK: we need to return the same instance, because it is all keep in same memory
        if (_store is null)
        {
            _store = new InProcessInMemStore(container.ResolveForUnshared<IQueueStoreNotificationHandler>()
                .ToOptional());
        }

        return _store;
    }
}
#endif