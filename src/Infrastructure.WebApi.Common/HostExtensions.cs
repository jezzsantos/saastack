using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Common.Recording;
using Domain.Interfaces.Entities;
using Infrastructure.Common.Recording;
using Infrastructure.WebApi.Common.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.WebApi.Common;

public static class HostExtensions
{
    /// <summary>
    ///     Configures a WebHost
    /// </summary>
    public static WebApplication ConfigureApiHost(this WebApplicationBuilder builder, SubDomainModules modules,
        RecorderOptions recorderOptions)
    {
        builder.Services.AddHttpContextAccessor();

        ConfigureConfiguration();
        ConfigureRecording();
        ConfigureAuthenticationAuthorization();
        ConfigureMultiTenancy();
        ConfigureApplicationServices();
        ConfigureWireFormats();
        ConfigureApiRequests();

        var app = builder.Build();

        app.EnableRequestRewind();
        app.AddExceptionShielding();

        modules.ConfigureHost(app);

        return app;

        void ConfigureConfiguration()
        {
            //TODO: register configuration classes
        }

        void ConfigureRecording()
        {
            builder.Services.AddSingleton(NullRecorder.Instance);
        }

        void ConfigureAuthenticationAuthorization()
        {
            //TODO: need to add authentication/authorization (https://www.youtube.com/watch?v=XKN0084p7WQ)
        }

        void ConfigureMultiTenancy()
        {
            //TODO: setup multi-tenancy
        }

        void ConfigureApplicationServices()
        {
            builder.Services.AddHttpClient();

            var factory = new HostIdentifierFactory(modules.AggregatePrefixes);
            builder.Services.AddSingleton<IIdentifierFactory>(factory);
            builder.Services.AddScoped<ICallerContext, AnonymousCallerContext>();
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
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.WriteIndented = false;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase,
                    false));
                options.SerializerOptions.Converters.Add(new JsonDateTimeConverter(DateFormat.Iso8601));
            });

            builder.Services.ConfigureHttpXmlOptions(options => { options.SerializerOptions.WriteIndented = false; });
        }
    }
}