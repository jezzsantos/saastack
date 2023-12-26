using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Authorization;
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
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Infrastructure.Web.Hosting.Common.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if TESTINGONLY
using Infrastructure.Persistence.Interfaces.ApplicationServices;
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
    private const string AllowedCORSOriginsSettingName = "Hosts:AllowedCORSOrigins";
    private const string CheckPointAggregatePrefix = "check";
    private const string LoggingSettingName = "Logging";
    private static readonly char[] AllowedCORSOriginsDelimiters = { ',', ';', ' ' };
    private static readonly Dictionary<string, IWebRequest> StubQueueDrainingServiceQueuedApiMappings = new()
    {
#if TESTINGONLY
        { "audits", new DrainAllAuditsRequest() },
        { "usages", new DrainAllUsagesRequest() }
        //     { "emails", new DrainAllEmailsRequest() },
        //     { "events", new DrainAllEventsRequest() }, 
#endif
    };

    /// <summary>
    ///     Configures a WebHost
    /// </summary>
    public static WebApplication ConfigureApiHost(this WebApplicationBuilder appBuilder, SubDomainModules modules,
        WebHostOptions hostOptions)
    {
        ConfigureSharedServices();
        ConfigureConfiguration(hostOptions.IsMultiTenanted);
        ConfigureRecording();
        ConfigureMultiTenancy(hostOptions.IsMultiTenanted);
        ConfigureAuthenticationAuthorization(hostOptions.UsesAuth);
        ConfigureWireFormats();
        ConfigureApiRequests();
        ConfigureApplicationServices();
        ConfigurePersistence(hostOptions.Persistence.UsesQueues);
        ConfigureCors(hostOptions.UsesCORS);

        var app = appBuilder.Build();

        app.EnableRequestRewind(); // Required by XMLHttpResult and HMACAuth
        app.EnableOtherOptions(hostOptions);
        app.AddExceptionShielding();
        //TODO: app.AddMultiTenancyDetection(); we need a TenantDetective
        app.EnableEventingListeners(hostOptions.Persistence.UsesEventing);
        app.EnableApiUsageTracking(hostOptions.TrackApiUsage);
        app.EnableCORS(hostOptions.CORS);
        app.EnableSecureAccess(hostOptions.UsesAuth); //Note: AuthN must be registered after CORS

        modules.ConfigureHost(app);

        return app;

        void ConfigureSharedServices()
        {
            appBuilder.Services.AddHttpContextAccessor();
        }

        void ConfigureConfiguration(bool isMultiTenanted)
        {
#if HOSTEDONAZURE
            appBuilder.Configuration.AddJsonFile("appsettings.Azure.json", true);
#endif
#if HOSTEDONAWS
            appBuilder.Configuration.AddJsonFile("appsettings.AWS.json", true);
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
                loggingBuilder.AddConfiguration(appBuilder.Configuration.GetSection(LoggingSettingName));
#if TESTINGONLY
                loggingBuilder.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "hh:mm:ss ";
                    options.SingleLine = true;
                    options.IncludeScopes = false;
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
                    hostOptions));
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
            appBuilder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.WriteIndented = false;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase,
                    false));
                options.SerializerOptions.Converters.Add(new JsonDateTimeConverter(DateFormat.Iso8601));
            });

            appBuilder.Services.ConfigureHttpXmlOptions(options =>
            {
                options.SerializerOptions.WriteIndented = false;
            });
        }

        void ConfigureApplicationServices()
        {
            appBuilder.Services.AddHttpClient();
            var prefixes = modules.AggregatePrefixes;
            prefixes.Add(typeof(Checkpoint), CheckPointAggregatePrefix);
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

        void ConfigureCors(CORSOption cors)
        {
            if (cors == CORSOption.None)
            {
                return;
            }

            appBuilder.Services.AddCors(options =>
            {
                if (cors == CORSOption.SameOrigin)
                {
                    var allowedOrigins = appBuilder.Configuration.GetValue<string>(AllowedCORSOriginsSettingName)
                                         ?? string.Empty;
                    if (allowedOrigins.HasNoValue())
                    {
                        throw new InvalidOperationException(
                            Resources.CORS_MissingSameOrigins.Format(AllowedCORSOriginsSettingName));
                    }

                    var origins = allowedOrigins.Split(AllowedCORSOriginsDelimiters);
                    options.AddDefaultPolicy(corsBuilder =>
                    {
                        corsBuilder.WithOrigins(origins);
                        corsBuilder.AllowAnyMethod();
                        corsBuilder.WithHeaders(HttpHeaders.ContentType, HttpHeaders.Authorization);
                        corsBuilder.DisallowCredentials();
                        corsBuilder.SetPreflightMaxAge(TimeSpan.FromSeconds(600));
                    });
                }

                if (cors == CORSOption.AnyOrigin)
                {
                    options.AddDefaultPolicy(corsBuilder =>
                    {
                        corsBuilder.AllowAnyOrigin();
                        corsBuilder.AllowAnyMethod();
                        corsBuilder.WithHeaders(HttpHeaders.ContentType, HttpHeaders.Authorization);
                        corsBuilder.DisallowCredentials();
                        corsBuilder.SetPreflightMaxAge(TimeSpan.FromSeconds(600));
                    });
                }
            });
        }

#if TESTINGONLY
        static void RegisterStoreForTestingOnly(WebApplicationBuilder appBuilder, bool usesQueues)
        {
            appBuilder.Services
                .RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.ResolveForUnshared<IConfigurationSettings>().Platform,
                        usesQueues
                            ? c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                            : null));
            //HACK: In TESTINGONLY there won't be any physical partitioning of data for different tenants,
            // even if the host is multi-tenanted. So we can register a singleton for this specific store,
            // as we only ever want to resolve one instance for this store for all its uses (tenanted or unshared, except for platform use)
            appBuilder.Services
                .RegisterUnshared<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.ResolveForUnshared<IConfigurationSettings>().Platform,
                        usesQueues
                            ? c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                            : null));
            if (usesQueues)
            {
                RegisterStubMessageQueueDrainingService(appBuilder);
            }
        }

        static void RegisterStubMessageQueueDrainingService(WebApplicationBuilder appBuilder)
        {
            appBuilder.Services.RegisterUnshared<IMonitoredMessageQueues, MonitoredMessageQueues>();
            appBuilder.Services.RegisterUnshared<IQueueStoreNotificationHandler, StubQueueStoreNotificationHandler>();
            appBuilder.Services.AddHostedService(services =>
                new StubQueueDrainingService(services.GetRequiredService<IHttpClientFactory>(),
                    services.ResolveForUnshared<IHostSettings>(),
                    services.GetRequiredService<ILogger<StubQueueDrainingService>>(),
                    services.ResolveForUnshared<IMonitoredMessageQueues>(), StubQueueDrainingServiceQueuedApiMappings));
        }
#endif
    }
}