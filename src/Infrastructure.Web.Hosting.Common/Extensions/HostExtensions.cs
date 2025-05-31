#define USEDATABASEEVENTSTORE //EXTEND: Set this to true if you are using a database event store in production

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Common.Projections.ReadModels;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.External.ApplicationServices;
using Infrastructure.Hosting.Common;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Hosting.Common.Recording;
using Infrastructure.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Auth;
using Infrastructure.Web.Hosting.Common.Documentation;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
#if !TESTINGONLY
#if HOSTEDONAZURE
using Infrastructure.External.Persistence.Azure.ApplicationServices;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
#elif HOSTEDONAWS
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Infrastructure.Persistence.Common.ApplicationServices;
#endif
#endif

namespace Infrastructure.Web.Hosting.Common.Extensions;

public static class HostExtensions
{
    private const string AllowedCORSOriginsSettingName = "Hosts:AllowedCORSOrigins";
    private const string CheckPointAggregatePrefix = "check";
    private const string LoggingSettingName = "Logging";
    private static readonly char[] AllowedCORSOriginsDelimiters = [',', ';', ' '];

    /// <summary>
    ///     Configures a WebHost
    /// </summary>
    public static WebApplication ConfigureApiHost(this WebApplicationBuilder appBuilder, SubdomainModules modules,
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
        RegisterApiDocumentation(hostOptions.HostName, hostOptions.HostVersion, hostOptions.UsesApiDocumentation);
        RegisterNotifications(hostOptions.UsesNotifications);
        modules.RegisterServices(appBuilder.Configuration, services);
        RegisterApplicationServices(hostOptions.IsBackendForFrontEnd, hostOptions.IsMultiTenanted,
            hostOptions.ReceivesWebhooks);
        RegisterPersistence(hostOptions.Persistence.UsesQueues, hostOptions.IsMultiTenanted);
        RegisterEventing(hostOptions.Persistence.UsesEventing);
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
            services.AddAntiforgery();
            services.AddHttpContextAccessor();

            // EXTEND: Default technology adapters
            services.AddSingleton<IFeatureFlags>(c =>
                new FlagsmithHttpServiceClient(c.GetRequiredService<IRecorder>(),
                    c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                    c.GetRequiredService<IHttpClientFactory>()));
        }

        void RegisterConfiguration(bool isMultiTenanted)
        {
#if HOSTEDONAZURE
            appBuilder.Configuration.AddJsonFile("appsettings.Azure.json", true);
#elif HOSTEDONAWS
            appBuilder.Configuration.AddJsonFile("appsettings.AWS.json", true);
#endif
            appBuilder.Configuration.AddJsonFile("appsettings.local.json", true);

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
#if HOSTEDONAZURE
#if !TESTINGONLY
            // Note: Apply sampling, but never sample any Exceptions
            services.Configure<TelemetryConfiguration>(config =>
            {
                var builder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
                builder.UseAdaptiveSampling(10, "Exception");
                builder.Build();
            });

            services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
            {
                // We always want this to be false
                // See https://learn.microsoft.com/en-us/azure/azure-monitor/app/sampling-classic-api#configure-sampling-settings
                EnableAdaptiveSampling = false
            });
#endif
#elif HOSTEDONAWS
#if !TESTINGONLY
            AWSXRayRecorder.InitializeInstance(appBuilder.Configuration);
            AWSSDKHandler.RegisterXRayForAllServices();
#endif
            services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
#endif
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConfiguration(appBuilder.Configuration.GetSection(LoggingSettingName));
#if TESTINGONLY
                builder.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "hh:mm:ss ";
                    options.SingleLine = true;
                    options.IncludeScopes = false;
                });
                builder.AddDebug();
#else
#if HOSTEDONAZURE
                builder.AddApplicationInsights();
#elif HOSTEDONAWS
                builder.AddLambdaLogger();
#endif
#endif
                builder.AddEventSourceLogger();
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
            else
            {
                services.AddSingleton<ITenancyContext, NoOpTenancyContext>();
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
                authBuilder.AddScheme<PrivateInterHostOptions, PrivateInterHostAuthenticationHandler>(
                    PrivateInterHostAuthenticationHandler.AuthenticationScheme,
                    _ => { });
                services.AddAuthorization(configure =>
                {
                    configure.AddPolicy(AuthenticationConstants.Authorization.PrivateInterHostPolicyName, builder =>
                    {
                        builder.AddAuthenticationSchemes(PrivateInterHostAuthenticationHandler.AuthenticationScheme);
                        builder.RequireAuthenticatedUser();
                    });
                });

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
            services.RegisterFluentValidators(modules.ApiAssemblies);
        }

        void RegisterApiDocumentation(string name, string version, bool usesApiDocumentation)
        {
            if (!usesApiDocumentation)
            {
                return;
            }

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.ParameterFilter<DataAnnotationsParameterFilter>();
                options.SchemaFilter<AllSchemaFilter>();
                options.OperationFilter<BugFixParameterOperationFilter>(options);
                options.OperationFilter<RouteTemplateParameterFilter>();
                options.OperationFilter<DefaultBodyFilter>();
                options
                    .OperationFilter<
                        XmlDocumentationOperationFilter>(); // must declare before the DefaultResponsesFilter
                options.OperationFilter<DefaultResponsesFilter>();
                options.OperationFilter<RouteAuthenticationSecurityFilter>();
                options.SwaggerDoc(version, new OpenApiInfo
                {
                    Version = version,
                    Title = name,
                    Description = name
                });

                if (hostOptions.Authorization.UsesTokens)
                {
                    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        Name = HttpConstants.Headers.Authorization,
                        Description =
                            Resources.HostExtensions_ApiDocumentation_TokenDescription.Format(JwtBearerDefaults
                                .AuthenticationScheme),
                        In = ParameterLocation.Header,
                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                        BearerFormat = "JWT"
                    });
                }

                if (hostOptions.Authorization.UsesApiKeys)
                {
                    options.AddSecurityDefinition(APIKeyAuthenticationHandler.AuthenticationScheme,
                        new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.ApiKey,
                            Name = HttpConstants.QueryParams.APIKey,
                            Description =
                                Resources.HostExtensions_ApiDocumentation_APIKeyQueryDescription.Format(HttpConstants.QueryParams
                                    .APIKey),
                            In = ParameterLocation.Query,
                            Scheme = APIKeyAuthenticationHandler.AuthenticationScheme
                        });
                }
            });
        }

        void RegisterNotifications(bool usesNotifications)
        {
            if (usesNotifications)
            {
                services.AddSingleton<IEmailMessageQueue>(c =>
                    new EmailMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<ISmsMessageQueue>(c =>
                    new SmsMessageQueue(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IMessageQueueMessageIdFactory>(),
                        c.GetRequiredServiceForPlatform<IQueueStore>()));
                services.AddSingleton<IEmailSchedulingService, QueuingEmailSchedulingService>();
                services.AddSingleton<ISmsSchedulingService, QueuingSmsSchedulingService>();
                services.AddSingleton<IWebsiteUiService, WebsiteUiService>();
                services.AddSingleton<IUserNotificationsService>(c =>
                    new MessageUserNotificationsService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<IHostSettings>(), c.GetRequiredService<IWebsiteUiService>(),
                        c.GetRequiredService<IEmailSchedulingService>(),
                        c.GetRequiredService<ISmsSchedulingService>()));
            }
        }

        void RegisterWireFormats()
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

        void RegisterApplicationServices(bool isBeffeHost, bool isMultiTenanted, bool receivesWebhooks)
        {
            services.AddHttpClient();
            var prefixes = modules.EntityPrefixes;
            prefixes.Add(typeof(Checkpoint), CheckPointAggregatePrefix);
            services.AddSingleton<IIdentifierFactory>(_ => new HostIdentifierFactory(prefixes));

            if (isBeffeHost)
            {
                services.AddPerHttpRequest<ICallerContextFactory, AspNetBeffeCallerFactory>();
            }
            else
            {
                services.AddPerHttpRequest<ICallerContextFactory, AspNetHttpContextCallerFactory>();
            }

            if (isMultiTenanted)
            {
                if (receivesWebhooks)
                {
                    services
                        .AddPerHttpRequest<IWebhookNotificationAuditRepository, WebhookNotificationAuditRepository>();
                    services.AddPerHttpRequest<IWebhookNotificationAuditService, WebhookNotificationAuditService>();
                }
            }
            else
            {
                if (receivesWebhooks)
                {
                    services.AddSingleton<IWebhookNotificationAuditRepository, WebhookNotificationAuditRepository>();
                    services.AddSingleton<IWebhookNotificationAuditService, WebhookNotificationAuditService>();
                }
            }
        }

        void RegisterPersistence(bool usesQueues, bool isMultiTenanted)
        {
            var domainAssemblies = modules.SubdomainAssemblies
                .Concat([typeof(DomainCommonMarker).Assembly, typeof(DomainSharedMarker).Assembly])
                .ToArray();

            services.AddForPlatform<IDependencyContainer, DotNetDependencyContainer>();
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IDependencyContainer, DotNetDependencyContainer>();
                services.AddPerHttpRequest<IDomainFactory>(c => DomainFactory.CreateRegistered(
                    c.GetRequiredService<IDependencyContainer>(), domainAssemblies));
            }
            else
            {
                services.AddSingleton<IDependencyContainer, DotNetDependencyContainer>();
                services.AddSingleton<IDomainFactory>(c => DomainFactory.CreateRegistered(
                    c.GetRequiredServiceForPlatform<IDependencyContainer>(), domainAssemblies));
            }

            services.AddSingleton<IMessageQueueMessageIdFactory, MessageQueueMessageIdFactory>();
            services.AddSingleton<IMessageBusTopicMessageIdFactory, MessageBusTopicMessageIdFactory>();
            services.AddSingleton<IEventSourcedChangeEventMigrator, ChangeEventTypeMigrator>();
#if TESTINGONLY
            TestingOnlyHostExtensions.RegisterStoreForTestingOnly(services, usesQueues, isMultiTenanted);
#else
#if HOSTEDONAZURE
            // EXTEND: Add your production persistence stores here
#if USEDATABASEEVENTSTORE
            services.AddForPlatform<IDataStore, IEventStore, AzureSqlServerStore>(c =>
                AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                    AzureSqlServerStoreOptions.Credentials(c.GetRequiredServiceForPlatform<IConfigurationSettings>())));
#else
                services.AddSingleton<IKurrentEventStoreClient>(c =>
                    new SharedKurrentEventStoreClient(c.GetRequiredServiceForPlatform<IConfigurationSettings>()));
                services.AddForPlatform<IEventStore>(c =>
                    KurrentEventStore.Create(c.GetRequiredService<IRecorder>(),
                        KurrentEventStoreOptions.SharedClient(c.GetRequiredService<IKurrentEventStoreClient>())));
                services.AddForPlatform<IDataStore>(c =>
                    AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureSqlServerStoreOptions.Credentials(c.GetRequiredServiceForPlatform<IConfigurationSettings>())));
#endif
            services.AddForPlatform<IBlobStore>(c =>
                AzureStorageAccountBlobStore.Create(c.GetRequiredService<IRecorder>(),
                    AzureStorageAccountStoreOptions.Credentials(
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>())));
            services.AddForPlatform<IQueueStore>(c =>
                AzureStorageAccountQueueStore.Create(c.GetRequiredService<IRecorder>(),
                    AzureStorageAccountStoreOptions.Credentials(
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>())));
            services.AddForPlatform<IMessageBusStore>(c =>
                AzureServiceBusStore.Create(c.GetRequiredService<IRecorder>(),
                    AzureServiceBusStoreOptions.Credentials(
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>())));

            if (isMultiTenanted)
            {
#if USEDATABASEEVENTSTORE
                services.AddPerHttpRequest<IDataStore, IEventStore, AzureSqlServerStore>(c =>
                    AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureSqlServerStoreOptions.Credentials(
                            c.GetRequiredServiceForPlatform<IConfigurationSettings>())));
#else
                    services.AddPerHttpRequest<IEventStore>(c =>
                        KurrentEventStore.Create(c.GetRequiredService<IRecorder>(),
                            KurrentEventStoreOptions.SharedClient(c.GetRequiredService<IKurrentEventStoreClient>())));
                    services.AddPerHttpRequest<IDataStore>(c =>
                        AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                            AzureSqlServerStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
#endif
                services.AddPerHttpRequest<IBlobStore>(c =>
                    AzureStorageAccountBlobStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureStorageAccountStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
                services.AddPerHttpRequest<IQueueStore>(c =>
                    AzureStorageAccountQueueStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureStorageAccountStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
                services.AddPerHttpRequest<IMessageBusStore>(c =>
                    AzureServiceBusStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureServiceBusStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
            }
            else
            {
#if USEDATABASEEVENTSTORE
                services.AddSingleton<IDataStore, IEventStore, AzureSqlServerStore>(c =>
                    AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureSqlServerStoreOptions.Credentials(
                            c.GetRequiredServiceForPlatform<IConfigurationSettings>())));
#else
                    services.AddSingleton<IEventStore>(c =>
                        KurrentEventStore.Create(c.GetRequiredService<IRecorder>(),
                            KurrentEventStoreOptions.SharedClient(c.GetRequiredService<IKurrentEventStoreClient>())));
                    services.AddSingleton<IDataStore>(c =>
                        AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                            AzureSqlServerStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
#endif
                services.AddSingleton<IBlobStore>(c =>
                    AzureStorageAccountBlobStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureStorageAccountStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
                services.AddSingleton<IQueueStore>(c =>
                    AzureStorageAccountQueueStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureStorageAccountStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
                services.AddSingleton<IMessageBusStore>(c =>
                    AzureServiceBusStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureServiceBusStoreOptions.Credentials(c.GetRequiredService<IConfigurationSettings>())));
            }
#elif HOSTEDONAWS
            //HACK: Need AWS production stores here like DynamoDbDataStore
            services.AddForPlatform<IDataStore, IEventStore, IBlobStore, IQueueStore, IMessageBusStore, NoOpStore>(_ =>
                NoOpStore.Instance);
            if (isMultiTenanted)
            {
                services.AddPerHttpRequest<IDataStore, IEventStore, IBlobStore, IQueueStore, IMessageBusStore, NoOpStore>(_ =>
                    NoOpStore.Instance);
            }
            else
            {
                services.AddSingleton<IDataStore, IEventStore, IBlobStore, IQueueStore, IMessageBusStore, NoOpStore>(_ =>
                    NoOpStore.Instance);
            }
#endif
#endif
        }

        void RegisterEventing(bool usesEventing)
        {
            if (usesEventing)
            {
                //EXTEND: Add support for other eventing mechanisms
                // Note: we are sending "domain events" via a message bus back to each ApiHost,
                // and sending "integration events" to some external message broker

                services.AddPerHttpRequest<IDomainEventConsumerRelay, AsynchronousQueueConsumerRelay>();
                services.AddPerHttpRequest<IEventNotificationMessageBroker, NoOpEventNotificationMessageBroker>();
            }
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
                        corsBuilder.WithHeaders(HttpConstants.Headers.ContentType, HttpConstants.Headers.Authorization);
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
                        corsBuilder.WithHeaders(HttpConstants.Headers.ContentType, HttpConstants.Headers.Authorization);
                        corsBuilder.DisallowCredentials();
                        corsBuilder.SetPreflightMaxAge(TimeSpan.FromSeconds(600));
                    });
                }
            });
        }
    }
}