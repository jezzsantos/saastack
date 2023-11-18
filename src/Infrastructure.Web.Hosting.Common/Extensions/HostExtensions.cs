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
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        modules.ConfigureHost(app);

        return app;

        void ConfigureSharedServices()
        {
            builder.Services.AddHttpContextAccessor();
        }

        void ConfigureRecording()
        {
            builder.Services.AddSingleton<IRecorder>(c =>
                new TracingOnlyRecorder(options.HostName,
                    c.GetRequiredService<ILoggerFactory>())); // TODO: we need a more comprehensive HostRecorder using Azure or AWS or GC
        }

        void ConfigureMultiTenancy(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                builder.Services.AddScoped<ITenancyContext, SimpleTenancyContext>();
            }
        }

        void ConfigureConfiguration(bool isMultiTenanted)
        {
            if (isMultiTenanted)
            {
                builder.Services.AddSingleton<ITenantSettingsService, AspNetHostLocalFileTenantSettingsService>();
                builder.Services.AddSingleton<ITenantSettingService>(c => new TenantSettingService(
                    new AesEncryptionService(c
                        .GetRequiredService<IConfigurationSettings>().Platform
                        .GetString(TenantSettingService.EncryptionServiceSecretSettingName))));
                builder.Services.AddScoped<IConfigurationSettings>(c =>
                    new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>(),
                        c.GetRequiredService<ITenancyContext>()));
            }
            else
            {
                builder.Services.AddSingleton<IConfigurationSettings>(c =>
                    new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>()));
            }

            builder.Services.AddSingleton<IApiHostSetting>(c =>
                new WebApiHostSettings(new AspNetConfigurationSettings(c.GetRequiredService<IConfiguration>())));
        }

        void ConfigureAuthenticationAuthorization()
        {
            //TODO: need to add authentication/authorization (https://www.youtube.com/watch?v=XKN0084p7WQ)
        }

        void ConfigureApiRequests()
        {
            builder.Services.AddSingleton<IHasSearchOptionsValidator, HasSearchOptionsValidator>();
            builder.Services.AddSingleton<IHasGetOptionsValidator, HasGetOptionsValidator>();
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

            var factory = new HostIdentifierFactory(modules.AggregatePrefixes);
            builder.Services.AddSingleton<IIdentifierFactory>(factory);
            builder.Services.AddScoped<ICallerContext, AnonymousCallerContext>();
        }

        void ConfigurePersistence()
        {
            var domainAssemblies = modules.DomainAssemblies
                .Concat(new[] { typeof(DomainCommonMarker).Assembly })
                .ToArray();
            builder.Services.AddSingleton<IDependencyContainer>(c => new DotNetDependencyContainer(c));
            builder.Services.AddSingleton<IDomainFactory>(c => DomainFactory.CreateRegistered(
                c.GetRequiredService<IDependencyContainer>(), domainAssemblies));
            builder.Services.AddSingleton<IEventSourcedChangeEventMigrator, ChangeEventTypeMigrator>();

#if TESTINGONLY
            builder.Services
                .AddSingleton<IDataStore, IEventStore, IBlobStore, IQueueStore, InProcessInMemStoreTrigger>(c =>
                    new InProcessInMemStoreTrigger(c.GetRequiredService<IQueueStoreNotificationHandler>()
                        .ToOptional()));
            RegisterStubMessageQueueDrainingService();

#else
            //HACK: we need a reasonable value for production here like SQLServerDataStore
            builder.Services.AddSingleton<IDataStore>(c=> NullStore.Instance);
#endif
        }

#if TESTINGONLY
        void RegisterStubMessageQueueDrainingService()
        {
            builder.Services.AddSingleton<IMonitoredMessageQueues, MonitoredMessageQueues>();
            builder.Services.AddSingleton<IQueueStoreNotificationHandler, StubQueueStoreNotificationHandler>();
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
                    services.GetRequiredService<IApiHostSetting>(),
                    services.GetRequiredService<ILogger<StubQueueDrainingService>>(),
                    services.GetRequiredService<IMonitoredMessageQueues>(), drainApiMappings));
        }
#endif
    }
}