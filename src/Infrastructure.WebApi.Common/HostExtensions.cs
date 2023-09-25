using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Common.Recording;
using Domain.Interfaces.Entities;
using Infrastructure.Common.Recording;
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
        SetupConfiguration();
        SetupRecording();
        SetupAuthenticationAuthorization();
        SetupMultiTenancy();
        SetupApplicationServices();
        SetupWireFormats();
        SetupApiRequests();

        var app = builder.Build();

        app.AddExceptionShielding();

        modules.ConfigureHost(app);

        return app;

        void SetupConfiguration()
        {
            //TODO: register configuration classes
        }

        void SetupRecording()
        {
            builder.Services.AddSingleton(NullRecorder.Instance);
        }

        void SetupAuthenticationAuthorization()
        {
            //TODO: need to add authentication/authorization (https://www.youtube.com/watch?v=XKN0084p7WQ)
        }

        void SetupMultiTenancy()
        {
            //TODO: setup multi-tenancy
        }

        void SetupApplicationServices()
        {
            var factory = new HostIdentifierFactory(modules.AggregatePrefixes);
            builder.Services.AddSingleton<IIdentifierFactory>(factory);
            builder.Services.AddScoped<ICallerContext, AnonymousCallerContext>();
        }

        void SetupApiRequests()
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.RegisterValidators(modules.ApiAssemblies, out var validators);

            builder.Services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssemblies(modules.ApiAssemblies.ToArray())
                    .AddValidatorBehaviors(validators, modules.ApiAssemblies);
            });
            modules.RegisterServices(builder.Configuration, builder.Services);
        }

        void SetupWireFormats()
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