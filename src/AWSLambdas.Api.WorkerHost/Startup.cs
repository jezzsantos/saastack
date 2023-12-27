using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if !TESTINGONLY
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
#endif

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace AWSLambdas.Api.WorkerHost;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile("appsettings.local.json", true, false)
            .AddEnvironmentVariables();
        var configuration = configurationBuilder.Build();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConfiguration(configuration.GetSection("Logging"));
#if TESTINGONLY
            builder.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "hh:mm:ss ";
                options.SingleLine = true;
                options.IncludeScopes = false;
            });
            builder.AddDebug();
#else
#if HOSTEDONAWS
                AWSXRayRecorder.InitializeInstance(configuration);
                AWSSDKHandler.RegisterXRayForAllServices();
                builder.AddLambdaLogger();
#endif
#endif
            builder.AddEventSourceLogger();
        });
        services.AddDependencies(configuration);
    }
}