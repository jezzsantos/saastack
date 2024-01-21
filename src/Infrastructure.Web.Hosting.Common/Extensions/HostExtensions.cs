using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Shared;
using Infrastructure.Common;
using Infrastructure.Common.DomainServices;
using Infrastructure.Common.Extensions;
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
using Microsoft.AspNetCore.Authentication.Cookies;
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
        { "audits", new DrainAllAuditsRequest() },
        { "usages", new DrainAllUsagesRequest() },
        { "emails", new DrainAllEmailsRequest() }
        //     { "events", new DrainAllEventsRequest() }, 
    };
#endif

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
        ConfigureAuthenticationAuthorization(hostOptions.Authorization);
        ConfigureWireFormats();
        ConfigureApiRequests();
        ConfigureApplicationServices();
        ConfigurePersistence(hostOptions.Persistence.UsesQueues);
        ConfigureCors(hostOptions.CORS);

        var app = appBuilder.Build();

        // Note: The order of the middleware matters
        app.EnableRequestRewind(); // Required by ContentNegotiationFilter and by HMACVerification
        app.EnableCORS(hostOptions.CORS);
        app.EnableSecureAccess(hostOptions.Authorization); //Note: AuthN must be registered after CORS
        app.AddExceptionShielding();
        app.EnableMultiTenancy(hostOptions.IsMultiTenanted);
        app.EnableEventingListeners(hostOptions.Persistence.UsesEventing);
        app.EnableApiUsageTracking(hostOptions.TrackApiUsage);
        app.EnableOtherOptions(hostOptions);

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

        void ConfigureAuthenticationAuthorization(AuthorizationOptions authentication)
        {
            if (authentication.HasNone)
            {
                return;
            }

            var defaultScheme = string.Empty;
            if (authentication.UsesCookies)
            {
                defaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }

            if (authentication.UsesTokens)
            {
                defaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }

            var onlyHMAC = authentication is
                { UsesHMAC: true, UsesCookies: false, UsesTokens: false, UsesApiKeys: false };
            var onlyApiKey = authentication is
                { UsesApiKeys: true, UsesCookies: false, UsesTokens: false, UsesHMAC: false };
            if (onlyHMAC || onlyApiKey)
            {
                // This is necessary in some versions of dotnet so that the only scheme is not applied to all endpoints by default
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

            if (authentication.UsesCookies)
            {
                //TODO: Is this how we are going to reverse proxy the cookie?
                //TODO: What about the API to relay logins requests to the backend, and manage refresh etc?
                // https://auth0.com/blog/building-a-reverse-proxy-in-dot-net-core/
                authBuilder.AddCookie(cookieOptions =>
                {
                    cookieOptions.LoginPath = "/api/user/login";
                    cookieOptions.LogoutPath = "/api/user/logout";
                });
            }

            appBuilder.Services.AddAuthorization();
            appBuilder.Services.AddSingleton<IAuthorizationHandler, RolesAndFeaturesAuthorizationHandler>();
            appBuilder.Services
                .AddSingleton<IAuthorizationPolicyProvider, RolesAndFeaturesAuthorizationPolicyProvider>();

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

        void ConfigureApplicationServices()
        {
            appBuilder.Services.AddHttpClient();
            var prefixes = modules.AggregatePrefixes;
            prefixes.Add(typeof(Checkpoint), CheckPointAggregatePrefix);
            appBuilder.Services.RegisterUnshared<IIdentifierFactory>(_ => new HostIdentifierFactory(prefixes));
            appBuilder.Services.AddSingleton<ICallerContextFactory, AspNetCallerContextFactory>();
        }

        void ConfigurePersistence(bool usesQueues)
        {
            if (usesQueues)
            {
                appBuilder.Services.RegisterUnshared<IMessageQueueIdFactory, MessageQueueIdFactory>();
            }

            var domainAssemblies = modules.DomainAssemblies
                .Concat(new[] { typeof(DomainCommonMarker).Assembly, typeof(DomainSharedMarker).Assembly })
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