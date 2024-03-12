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
        var services = appBuilder.Services;
        RegisterSharedServices();
        RegisterConfiguration(hostOptions.IsMultiTenanted);
        RegisterRecording();
        RegisterMultiTenancy(hostOptions.IsMultiTenanted);
        RegisterAuthenticationAuthorization(hostOptions.Authorization, hostOptions.IsMultiTenanted);
        RegisterWireFormats();
        RegisterApiRequests();
        RegisterNotifications(hostOptions.UsesNotifications);
        modules.RegisterServices(appBuilder.Configuration, services);
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
            services.AddHttpContextAccessor();
            services.AddSingleton<IFeatureFlags>(c =>
                new FlagsmithHttpServiceClient(c.GetRequiredService<IRecorder>(),
                    c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                    c.GetRequiredService<IHttpClientFactory>()));
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
                services.AddPerHttpRequest<IConfigurationSettings>(c =>
                    new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>(),
                        c.GetRequiredService<ITenancyContext>()));
            }
            else
            {
                services.AddSingleton<IConfigurationSettings>(c =>
                    new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            }

            services.AddForPlatform<IConfigurationSettings>(c =>
                new AspNetDynamicConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            services.AddSingleton<IHostSettings>(c =>
                new HostSettings(c.GetRequiredServiceForPlatform<IConfigurationSettings>()));
        }

        void RegisterRecording()
        {
#if HOSTEDONAWS
#if !TESTINGONLY
            AWSXRayRecorder.InitializeInstance(appBuilder.Configuration);
            AWSSDKHandler.RegisterXRayForAllServices();
#endif
            services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
#endif
            services.AddLogging(loggingBuilder =>
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

                services.AddApplicationInsightsTelemetry();
#elif HOSTEDONAWS
                loggingBuilder.AddLambdaLogger();
#endif
#endif
                loggingBuilder.AddEventSourceLogger();
            });

            // Note: IRecorder should always be not tenanted
            services.AddSingleton<IRecorder>(c =>
                new HostRecorder(c.GetRequiredServiceForPlatform<IDependencyContainer>(),
                    c.GetRequiredService<ILoggerFactory>(),
                    hostOptions));
        }

        void RegisterMultiTenancy(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<ITenancyContext, SimpleTenancyContext>();
                services.AddPerHttpRequest<ITenantDetective, RequestTenantDetective>();
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
                ? services.AddAuthentication(defaultScheme)
                : services.AddAuthentication();

            if (authentication.UsesHMAC)
            {
                authBuilder.AddScheme<HMACOptions, HMACAuthenticationHandler>(
                    HMACAuthenticationHandler.AuthenticationScheme,
                    _ => { });
                services.AddAuthorization(configure =>
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

            services.AddAuthorization();
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IAuthorizationHandler, RolesAndFeaturesAuthorizationHandler>();
            }
            else
            {
                services.AddSingleton<IAuthorizationHandler, RolesAndFeaturesAuthorizationHandler>();
            }

            services
                .AddSingleton<IAuthorizationPolicyProvider, RolesAndFeaturesAuthorizationPolicyProvider>();

            if (authentication.UsesApiKeys || authentication.UsesTokens)
            {
                services.AddAuthorization(configure =>
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
            services.AddSingleton<IHasSearchOptionsValidator, HasSearchOptionsValidator>();
            services.AddSingleton<IHasGetOptionsValidator, HasGetOptionsValidator>();
            services.RegisterValidators(modules.ApiAssemblies, out var validators);

            services.AddMediatR(configuration =>
            {
                // Note: Here we want to register MediatR handlers in Transient lifetime, so that any services resolved within the handlers
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
                services.AddSingleton<IEmailMessageQueue>(c =>
                    new EmailMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IMessageQueueIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<IEmailSchedulingService, QueuingEmailSchedulingService>();
                services.AddSingleton<IWebsiteUiService, WebsiteUiService>();
                services.AddSingleton<INotificationsService>(c =>
                    new EmailNotificationsService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<IHostSettings>(), c.GetRequiredService<IWebsiteUiService>(),
                        c.GetRequiredService<IEmailSchedulingService>()));
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

            services.AddSingleton(serializerOptions);
            services.ConfigureHttpJsonOptions(options =>
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

            services.ConfigureHttpXmlOptions(options => { options.SerializerOptions.WriteIndented = false; });
        }

        void RegisterApplicationServices(bool isMultiTenanted)
        {
            services.AddHttpClient();
            var prefixes = modules.AggregatePrefixes;
            prefixes.Add(typeof(Checkpoint), CheckPointAggregatePrefix);
            services.AddSingleton<IIdentifierFactory>(_ => new HostIdentifierFactory(prefixes));

            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<ICallerContextFactory, AspNetCallerContextFactory>();
            }
            else
            {
                services.AddSingleton<ICallerContextFactory, AspNetCallerContextFactory>();
            }
        }

        void RegisterPersistence(bool usesQueues, bool isMultiTenanted)
        {
            var domainAssemblies = modules.DomainAssemblies
                .Concat(new[] { typeof(DomainCommonMarker).Assembly, typeof(DomainSharedMarker).Assembly })
                .ToArray();

            services.AddForPlatform<IDependencyContainer, DotNetDependencyContainer>();
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IDependencyContainer, DotNetDependencyContainer>();
            }
            else
            {
                services.AddSingleton<IDependencyContainer, DotNetDependencyContainer>();
            }

            services.AddSingleton<IMessageQueueIdFactory, MessageQueueIdFactory>();
            services.AddSingleton<IDomainFactory>(c => DomainFactory.CreateRegistered(
                c.GetRequiredServiceForPlatform<IDependencyContainer>(), domainAssemblies));
            services.AddSingleton<IEventSourcedChangeEventMigrator, ChangeEventTypeMigrator>();

#if TESTINGONLY
            RegisterStoreForTestingOnly(services, usesQueues, isMultiTenanted);
#else
            //HACK: we need a reasonable value for production here like SQLServerDataStore
            services.AddForPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
                NullStore.Instance);
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
                    NullStore.Instance);
            }
            else
            {
                services.AddSingleton<IDataStore, IEventStore, IBlobStore, IQueueStore, NullStore>(_ =>
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

            services.AddCors(options =>
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
        static void RegisterStoreForTestingOnly(IServiceCollection services, bool usesQueues, bool isMultiTenanted)
        {
            services
                .AddForPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                    LocalMachineJsonFileStore.Create(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        usesQueues
                            ? c.GetRequiredService<IQueueStoreNotificationHandler>()
                            : null));
            if (isMultiTenanted)
            {
                services
                    .AddPerHttpRequest<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                        LocalMachineJsonFileStore.Create(c.GetRequiredService<IConfigurationSettings>(),
                            usesQueues
                                ? c.GetRequiredService<IQueueStoreNotificationHandler>()
                                : null));
            }
            else
            {
                services
                    .AddSingleton<IDataStore, IEventStore, IBlobStore, IQueueStore, LocalMachineJsonFileStore>(c =>
                        LocalMachineJsonFileStore.Create(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                            usesQueues
                                ? c.GetRequiredService<IQueueStoreNotificationHandler>()
                                : null));
            }

            if (usesQueues)
            {
                RegisterStubMessageQueueDrainingService(services);
            }
        }

        static void RegisterStubMessageQueueDrainingService(IServiceCollection services)
        {
            services.AddSingleton<IMonitoredMessageQueues, MonitoredMessageQueues>();
            services.AddSingleton<IQueueStoreNotificationHandler, StubQueueStoreNotificationHandler>();
            services.AddHostedService(c =>
                new StubQueueDrainingService(c.GetRequiredService<IHttpClientFactory>(),
                    c.GetRequiredService<JsonSerializerOptions>(),
                    c.GetRequiredService<IHostSettings>(),
                    c.GetRequiredService<ILogger<StubQueueDrainingService>>(),
                    c.GetRequiredService<IMonitoredMessageQueues>(), StubQueueDrainingServiceQueuedApiMappings));
        }
#endif
    }
}