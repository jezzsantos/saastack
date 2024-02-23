using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.FeatureFlags;
using Domain.Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Infrastructure.Common;
using Infrastructure.Common.Extensions;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Infrastructure.Hosting.Common;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Hosting.Common.Recording;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Common.ApplicationServices;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Infrastructure.Web.Hosting.Common.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
#if TESTINGONLY
    private static readonly Dictionary<string, IWebRequest> StubQueueDrainingServiceQueuedApiMappings = new()
    {
        { WorkerConstants.Queues.Audits, new DrainAllAuditsRequest() },
        { WorkerConstants.Queues.Usages, new DrainAllUsagesRequest() },
        { WorkerConstants.Queues.Emails, new DrainAllEmailsRequest() },
        { WorkerConstants.Queues.Provisionings, new DrainAllProvisioningsRequest() }
        //     { "events", new DrainAllEventsRequest() }, 
    };
#endif

    /// <summary>
    ///     Configures a WebHost
    /// </summary>
    public static WebApplication ConfigureApiHost(this WebApplicationBuilder appBuilder, SubDomainModules modules,
        WebHostOptions hostOptions)
    {
        RegisterSharedServices();
        RegisterConfiguration(hostOptions.IsMultiTenanted);
        RegisterRecording();
        RegisterMultiTenancy(hostOptions.IsMultiTenanted);
        RegisterAuthenticationAuthorization(hostOptions.Authorization, hostOptions.IsMultiTenanted);
        RegisterWireFormats();
        RegisterApiRequests();
        RegisterNotifications(hostOptions.UsesNotifications);
        modules.RegisterServices(appBuilder.Configuration, appBuilder.Services);
        RegisterApplicationServices(hostOptions.IsMultiTenanted);
        RegisterPersistence(hostOptions.Persistence.UsesQueues, hostOptions.IsMultiTenanted);
        RegisterCors(hostOptions.CORS);

        var app = appBuilder.Build();

        // Note: The order of the middleware matters!
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0#middleware-order
        var middlewares = new List<MiddlewareRegistration>();
        app.EnableRequestRewind(middlewares);
        app.AddExceptionShielding(middlewares);
        app.AddBEFFE(middlewares, hostOptions.IsBackendForFrontEnd);
        app.EnableCORS(middlewares, hostOptions.CORS);
        app.EnableSecureAccess(middlewares, hostOptions.Authorization);
        app.EnableMultiTenancy(middlewares, hostOptions.IsMultiTenanted);
        app.EnableEventingPropagation(middlewares, hostOptions.Persistence.UsesEventing);
        app.EnableOtherFeatures(middlewares, hostOptions);

        modules.ConfigureMiddleware(app, middlewares);

        middlewares
            .OrderBy(mw => mw.Priority)
            .ToList()
            .ForEach(mw => mw.Register(app));

        return app;

        void RegisterSharedServices()
        {
            appBuilder.Services.AddHttpContextAccessor();
            appBuilder.Services.AddSingleton<IFeatureFlags>(c =>
                new FlagsmithHttpServiceClient(c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForPlatform<IConfigurationSettings>(), c.ResolveForUnshared<IHttpClientFactory>()));
        }

        void RegisterConfiguration(bool isMultiTenanted)
        {
#if HOSTEDONAZURE
            appBuilder.Configuration.AddJsonFile("appsettings.Azure.json", true);
#endif
#if HOSTEDONAWS
            appBuilder.Configuration.AddJsonFile("appsettings.AWS.json", true);
#endif

            if (isMultiTenanted)
            {
                appBuilder.Services.RegisterTenanted<IConfigurationSettings>(c =>
                    new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>(),
                        c.ResolveForTenant<ITenancyContext>()));
            }
            else
            {
                appBuilder.Services.RegisterUnshared<IConfigurationSettings>(c =>
                    new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            }

            appBuilder.Services.RegisterPlatform<IConfigurationSettings>(c =>
                new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            appBuilder.Services.RegisterUnshared<IHostSettings>(c =>
                new HostSettings(c.ResolveForPlatform<IConfigurationSettings>()));
        }

        void RegisterRecording()
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

            // Note: IRecorder should always be not tenanted
            appBuilder.Services.RegisterUnshared<IRecorder>(c =>
                new HostRecorder(c.ResolveForPlatform<IDependencyContainer>(), c.ResolveForUnshared<ILoggerFactory>(),
                    hostOptions));
        }

        void RegisterMultiTenancy(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                appBuilder.Services.RegisterTenanted<ITenancyContext, SimpleTenancyContext>();
                appBuilder.Services.RegisterTenanted<ITenantDetective, RequestTenantDetective>();
            }
        }

        void RegisterAuthenticationAuthorization(AuthorizationOptions authentication, bool isMultiTenanted)
        {
            if (authentication.HasNone)
            {
                return;
            }

            var defaultScheme = string.Empty;
            if (authentication.UsesTokens)
            {
                defaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }

            var onlyHMAC = authentication is
                { UsesHMAC: true, UsesTokens: false, UsesApiKeys: false };
            var onlyApiKey = authentication is
                { UsesApiKeys: true, UsesTokens: false, UsesHMAC: false };
            if (onlyHMAC || onlyApiKey)
            {
                // Note: This is necessary in some versions of dotnet so that the only scheme is not applied to all endpoints by default
                AppContext.SetSwitch("Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme", true);
            }

            var authBuilder = defaultScheme.HasValue()
                ? appBuilder.Services.AddAuthentication(defaultScheme)
                : appBuilder.Services.AddAuthentication();

            if (authentication.UsesHMAC)
            {
                authBuilder.AddScheme<HMACOptions, HMACAuthenticationHandler>(
                    HMACAuthenticationHandler.AuthenticationScheme,
                    _ => { });
                appBuilder.Services.AddAuthorization(configure =>
                {
                    configure.AddPolicy(AuthenticationConstants.Authorization.HMACPolicyName, builder =>
                    {
                        builder.AddAuthenticationSchemes(HMACAuthenticationHandler.AuthenticationScheme);
                        builder.RequireAuthenticatedUser();
                        builder.RequireRole(ClaimExtensions.ToPlatformClaimValue(PlatformRoles.ServiceAccount));
                    });
                });
            }

            if (authentication.UsesApiKeys)
            {
                authBuilder.AddScheme<APIKeyOptions, APIKeyAuthenticationHandler>(
                    APIKeyAuthenticationHandler.AuthenticationScheme,
                    _ => { });
            }

            if (authentication.UsesTokens)
            {
                var configuration = appBuilder.Configuration;
                authBuilder.AddJwtBearer(jwtOptions =>
                {
                    jwtOptions.MapInboundClaims = false;
                    jwtOptions.RequireHttpsMetadata = true;
                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        RoleClaimType = AuthenticationConstants.Claims.ForRole,
                        NameClaimType = AuthenticationConstants.Claims.ForId,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidAudience = configuration["Hosts:IdentityApi:BaseUrl"],
                        ValidIssuer = configuration["Hosts:IdentityApi:BaseUrl"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(configuration["Hosts:IdentityApi:JWT:SigningSecret"]!))
                    };
                });
            }

            appBuilder.Services.AddAuthorization();
            if (isMultiTenanted)
            {
                appBuilder.Services.RegisterTenanted<IAuthorizationHandler, RolesAndFeaturesAuthorizationHandler>();
            }
            else
            {
                appBuilder.Services.RegisterUnshared<IAuthorizationHandler, RolesAndFeaturesAuthorizationHandler>();
            }

            appBuilder.Services
                .RegisterUnshared<IAuthorizationPolicyProvider, RolesAndFeaturesAuthorizationPolicyProvider>();

            if (authentication.UsesApiKeys || authentication.UsesTokens)
            {
                appBuilder.Services.AddAuthorization(configure =>
                {
                    configure.AddPolicy(AuthenticationConstants.Authorization.TokenPolicyName, builder =>
                    {
                        builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme,
                            APIKeyAuthenticationHandler.AuthenticationScheme);
                        builder.RequireAuthenticatedUser();
                    });
                });
            }
        }

        void RegisterApiRequests()
        {
            appBuilder.Services.RegisterUnshared<IHasSearchOptionsValidator, HasSearchOptionsValidator>();
            appBuilder.Services.RegisterUnshared<IHasGetOptionsValidator, HasGetOptionsValidator>();
            appBuilder.Services.RegisterValidators(modules.ApiAssemblies, out var validators);

            appBuilder.Services.AddMediatR(configuration =>
            {
                // Here we want to register handlers in Transient lifetime, so that any services resolved within the handlers
                //can be singletons, scoped, or transient (and use the same scope the handler is resolved in).
                configuration.Lifetime = ServiceLifetime.Transient;
                configuration.RegisterServicesFromAssemblies(modules.ApiAssemblies.ToArray())
                    .AddValidatorBehaviors(validators, modules.ApiAssemblies);
            });
        }

        void RegisterNotifications(bool usesNotifications)
        {
            if (usesNotifications)
            {
                appBuilder.Services.RegisterUnshared<IEmailMessageQueue>(c =>
                    new EmailMessageQueue(c.Resolve<IRecorder>(), c.Resolve<IMessageQueueIdFactory>(),
                        c.ResolveForPlatform<IQueueStore>()));
                appBuilder.Services.RegisterUnshared<IEmailSchedulingService, QueuingEmailSchedulingService>();
                appBuilder.Services.RegisterUnshared<IWebsiteUiService, WebsiteUiService>();
                appBuilder.Services.RegisterUnshared<INotificationsService>(c =>
                    new EmailNotificationsService(c.ResolveForPlatform<IConfigurationSettings>(),
                        c.ResolveForUnshared<IHostSettings>(), c.ResolveForUnshared<IWebsiteUiService>(),
                        c.ResolveForUnshared<IEmailSchedulingService>()));
            }
        }

        void RegisterWireFormats()
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
            serializerOptions.Converters.Add(new JsonDateTimeConverter(DateFormat.Iso8601));

            appBuilder.Services.RegisterUnshared(serializerOptions);
            appBuilder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = serializerOptions.PropertyNameCaseInsensitive;
                options.SerializerOptions.PropertyNamingPolicy = serializerOptions.PropertyNamingPolicy;
                options.SerializerOptions.WriteIndented = serializerOptions.WriteIndented;
                options.SerializerOptions.DefaultIgnoreCondition = serializerOptions.DefaultIgnoreCondition;
                foreach (var converter in serializerOptions.Converters)
                {
                    options.SerializerOptions.Converters.Add(converter);
                }
            });

            appBuilder.Services.ConfigureHttpXmlOptions(options =>
            {
                options.SerializerOptions.WriteIndented = false;
            });
        }

        void RegisterApplicationServices(bool isMultiTenanted)
        {
            appBuilder.Services.AddHttpClient();
            var prefixes = modules.AggregatePrefixes;
            prefixes.Add(typeof(Checkpoint), CheckPointAggregatePrefix);
            appBuilder.Services.RegisterUnshared<IIdentifierFactory>(_ => new HostIdentifierFactory(prefixes));

            if (isMultiTenanted)
            {
                appBuilder.Services.RegisterTenanted<ICallerContextFactory, AspNetCallerContextFactory>();
            }
            else
            {
                appBuilder.Services.AddSingleton<ICallerContextFactory, AspNetCallerContextFactory>();
            }
        }

        void RegisterPersistence(bool usesQueues, bool isMultiTenanted)
        {
            var domainAssemblies = modules.DomainAssemblies
                .Concat(new[] { typeof(DomainCommonMarker).Assembly, typeof(DomainSharedMarker).Assembly })
                .ToArray();

            appBuilder.Services.RegisterPlatform<IDependencyContainer>(c => new DotNetDependencyContainer(c));
            if (isMultiTenanted)
            {
                appBuilder.Services.RegisterTenanted<IDependencyContainer>(c => new DotNetDependencyContainer(c));
            }
            else
            {
                appBuilder.Services.RegisterUnshared<IDependencyContainer>(c => new DotNetDependencyContainer(c));
            }

            appBuilder.Services.RegisterUnshared<IMessageQueueIdFactory, MessageQueueIdFactory>();
            appBuilder.Services.RegisterUnshared<IDomainFactory>(c => DomainFactory.CreateRegistered(
                c.ResolveForPlatform<IDependencyContainer>(), domainAssemblies));
            appBuilder.Services.RegisterUnshared<IEventSourcedChangeEventMigrator, ChangeEventTypeMigrator>();

#if TESTINGONLY
            RegisterStoreForTestingOnly(appBuilder, usesQueues, isMultiTenanted);
#else
            //HACK: we need a reasonable value for production here like SQLServerDataStore
            appBuilder.Services.RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
                NullStore.Instance);
            if (isMultiTenanted)
            {
                appBuilder.Services.RegisterTenanted<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
                    NullStore.Instance);
            }
            else
            {
                appBuilder.Services.RegisterUnshared<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
                    NullStore.Instance);
            }
#endif
        }

        void RegisterCors(CORSOption cors)
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
        static void RegisterStoreForTestingOnly(WebApplicationBuilder appBuilder, bool usesQueues, bool isMultiTenanted)
        {
            appBuilder.Services
                .RegisterPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.ResolveForPlatform<IConfigurationSettings>(),
                        usesQueues
                            ? c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                            : null));
            if (isMultiTenanted)
            {
                appBuilder.Services
                    .RegisterTenanted<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                        LocalMachineJsonFileStore.Create(c.ResolveForTenant<IConfigurationSettings>(),
                            usesQueues
                                ? c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                                : null));
            }
            else
            {
                appBuilder.Services
                    .RegisterUnshared<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                        LocalMachineJsonFileStore.Create(c.ResolveForPlatform<IConfigurationSettings>(),
                            usesQueues
                                ? c.ResolveForUnshared<IQueueStoreNotificationHandler>()
                                : null));
            }

            if (usesQueues)
            {
                RegisterStubMessageQueueDrainingService(appBuilder);
            }
        }

        static void RegisterStubMessageQueueDrainingService(WebApplicationBuilder appBuilder)
        {
            appBuilder.Services.RegisterUnshared<IMonitoredMessageQueues, MonitoredMessageQueues>();
            appBuilder.Services.RegisterUnshared<IQueueStoreNotificationHandler, StubQueueStoreNotificationHandler>();
            appBuilder.Services.AddHostedService(c =>
                new StubQueueDrainingService(c.GetRequiredService<IHttpClientFactory>(),
                    c.GetRequiredService<JsonSerializerOptions>(),
                    c.ResolveForUnshared<IHostSettings>(),
                    c.GetRequiredService<ILogger<StubQueueDrainingService>>(),
                    c.ResolveForUnshared<IMonitoredMessageQueues>(), StubQueueDrainingServiceQueuedApiMappings));
        }
#endif
    }
}