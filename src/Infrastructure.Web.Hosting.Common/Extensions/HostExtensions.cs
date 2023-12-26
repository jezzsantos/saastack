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
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if TESTINGONLY
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
#else
#if HOSTEDONAZURE
using Microsoft.ApplicationInsights.Extensibility;
#elif HOSTEDONAWS
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
#endif
#endif

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class HostExtensions
{
    /// <summary>
    ///     Configures a WebHost
    /// </summary>
    public static WebApplication ConfigureApiHost(this WebApplicationBuilder appBuilder, SubDomainModules modules,
        WebHostOptions options)
    {
        ConfigureSharedServices();
        ConfigureConfiguration(options.IsMultiTenanted);
        ConfigureRecording();
        ConfigureMultiTenancy(options.IsMultiTenanted);
        ConfigureAuthenticationAuthorization();
        ConfigureWireFormats();
        ConfigureApiRequests();
        ConfigureApplicationServices();
        ConfigurePersistence(options.Persistence.UsesQueues);

        var app = appBuilder.Build();

        app.EnableRequestRewind();
        app.AddExceptionShielding();
        //TODO: app.AddMultiTenancyDetection(); we need a TenantDetective
        app.AddEventingListeners(options.Persistence.UsesEventing);
        app.EnableApiUsageTracking(options.TrackApiUsage);
        //TODO: add the HealthCheck endpoint
        //TODO: enable CORS

        modules.ConfigureHost(app);

        return app;

        void ConfigureSharedServices()
        {
            appBuilder.Services.AddHttpContextAccessor();
        }

        void ConfigureConfiguration(bool isMultiTenanted)
        {
#if !TESTINGONLY
#if HOSTEDONAZURE
            appBuilder.Configuration.AddJsonFile("appsettings.Azure.json", true);
#endif
#if HOSTEDONAWS
            appBuilder.Configuration.AddJsonFile("appsettings.AWS.json", true);
#endif
#endif

            if (isMultiTenanted)
            {
                appBuilder.Services
                    .RegisterUnshared<ITenantSettingsService, AspNetHostLocalFileTenantSettingsService>();
                appBuilder.Services.RegisterUnshared<ITenantSettingService>(c => new TenantSettingService(
                    new AesEncryptionService(c
                        .ResolveForTenant<IConfigurationSettings>().Platform
                        .GetString(TenantSettingService.EncryptionServiceSecretSettingName))));
                appBuilder.Services.RegisterTenanted<IConfigurationSettings>(c =>
                    new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>(),
                        c.ResolveForTenant<ITenancyContext>()));
            }
            else
            {
                appBuilder.Services.RegisterUnshared<IConfigurationSettings>(c =>
                    new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            }

            appBuilder.Services.RegisterUnshared<IHostSettings>(c =>
                new HostSettings(new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>())));
        }

        void ConfigureRecording()
        {
#if HOSTEDONAWS
#if !TESTINGONLY
            AWSXRayRecorder.InitializeInstance(appBuilder.Configuration);
            AWSSDKHandler.RegisterXRayForAllServices();
#endif
            appBuilder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
#endif
            appBuilder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConfiguration(appBuilder.Configuration.GetSection("Logging"));
#if TESTINGONLY
                loggingBuilder.AddSimpleConsole(opts =>
                {
                    opts.TimestampFormat = "hh:mm:ss ";
                    opts.SingleLine = true;
                    opts.IncludeScopes = false;
                });
                loggingBuilder.AddDebug();
#else
#if HOSTEDONAZURE
                loggingBuilder.AddApplicationInsights();

                appBuilder.Services.AddApplicationInsightsTelemetry();
#elif HOSTEDONAWS
                loggingBuilder.AddLambdaLogger();
#endif
#endif
                loggingBuilder.AddEventSourceLogger();
            });

            appBuilder.Services.RegisterUnshared<IRecorder>(c =>
                new HostRecorder(c.ResolveForUnshared<IDependencyContainer>(), c.ResolveForUnshared<ILoggerFactory>(),
                    options));
        }

        void ConfigureMultiTenancy(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                appBuilder.Services.RegisterTenanted<ITenancyContext, SimpleTenancyContext>();
            }
        }

        void ConfigureAuthenticationAuthorization()
        {
            //TODO: need to add authentication/authorization (https://www.youtube.com/watch?v=XKN0084p7WQ)
        }

        void ConfigureApiRequests()
        {
            appBuilder.Services.RegisterUnshared<IHasSearchOptionsValidator, HasSearchOptionsValidator>();
            appBuilder.Services.RegisterUnshared<IHasGetOptionsValidator, HasGetOptionsValidator>();
            appBuilder.Services.RegisterValidators(modules.ApiAssemblies, out var validators);

            appBuilder.Services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssemblies(modules.ApiAssemblies.ToArray())
                    .AddValidatorBehaviors(validators, modules.ApiAssemblies);
            });
            modules.RegisterServices(appBuilder.Configuration, appBuilder.Services);
        }

        void ConfigureWireFormats()
        {
            appBuilder.Services.ConfigureHttpJsonOptions(opts =>
            {
                opts.SerializerOptions.PropertyNameCaseInsensitive = true;
                opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                opts.SerializerOptions.WriteIndented = false;
                opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase,
                    false));
                opts.SerializerOptions.Converters.Add(new JsonDateTimeConverter(DateFormat.Iso8601));
            });

            appBuilder.Services.ConfigureHttpXmlOptions(opts => { opts.SerializerOptions.WriteIndented = false; });
        }

        void ConfigureApplicationServices()
        {
            appBuilder.Services.AddHttpClient();
            var prefixes = modules.AggregatePrefixes;
            prefixes.Add(typeof(Checkpoint), "check");
            appBuilder.Services.RegisterUnshared<IIdentifierFactory>(_ => new HostIdentifierFactory(prefixes));
            appBuilder.Services.RegisterTenanted<ICallerContext, AnonymousCallerContext>();
        }

        void ConfigurePersistence(bool usesQueues)
        {
            var domainAssemblies = modules.DomainAssemblies
                .Concat(new[] { typeof(DomainCommonMarker).Assembly })
                .ToArray();
            appBuilder.Services.RegisterUnshared<IDependencyContainer>(c => new DotNetDependencyContainer(c));
            appBuilder.Services.RegisterUnshared<IDomainFactory>(c => DomainFactory.CreateRegistered(
                c.ResolveForUnshared<IDependencyContainer>(), domainAssemblies));
            appBuilder.Services.RegisterUnshared<IEventSourcedChangeEventMigrator, ChangeEventTypeMigrator>();

#if TESTINGONLY
            RegisterStoreForTestingOnly(appBuilder, usesQueues);
#else
            //HACK: we need a reasonable value for production here like SQLServerDataStore
            appBuilder.Services.RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
                NullStore.Instance);
            appBuilder.Services.RegisterTenanted<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
                NullStore.Instance);
#endif
        }

#if TESTINGONLY
        static void RegisterStoreForTestingOnly(WebApplicationBuilder appBuilder, bool usesQueues)
        {
            appBuilder.Services
                .RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.ResolveForUnshared<IConfigurationSettings>().Platform,
                        c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                            .ToOptional()));
            //HACK: In TESTINGONLY there won't be any physical partitioning of data for different tenants,
            // even if the host is multi-tenanted. So we can register a singleton for this specific store,
            // as we only ever want to resolve one instance for this store for all its uses (tenanted or unshared, except for platform use)
            appBuilder.Services
                .RegisterUnshared<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.ResolveForUnshared<IConfigurationSettings>().Platform,
                        c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                            .ToOptional()));
            if (usesQueues)
            {
                RegisterStubMessageQueueDrainingService(appBuilder);
            }
        }

        static void RegisterStubMessageQueueDrainingService(WebApplicationBuilder appBuilder)
        {
            appBuilder.Services.RegisterUnshared<IMonitoredMessageQueues, MonitoredMessageQueues>();
            appBuilder.Services.RegisterUnshared<IQueueStoreNotificationHandler, StubQueueStoreNotificationHandler>();
            var drainApiMappings = new Dictionary<string, IWebRequest>
            {
                { "audits", new DrainAllAuditsRequest() },
                { "usages", new DrainAllUsagesRequest() }
                //     { "emails", new DrainAllEmailsRequest() },
                //     { "events", new DrainAllEventsRequest() },
            };
            appBuilder.Services.AddHostedService(services =>
                new StubQueueDrainingService(services.GetRequiredService<IHttpClientFactory>(),
                    services.ResolveForUnshared<IHostSettings>(),
                    services.GetRequiredService<ILogger<StubQueueDrainingService>>(),
                    services.ResolveForUnshared<IMonitoredMessageQueues>(), drainApiMappings));
        }
#endif
    }
}