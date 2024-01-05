using System.Text.Json;
using Application.Interfaces.Services;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Configuration;
using Common.Recording;
using Infrastructure.Common.Recording;
using Infrastructure.Hosting.Common;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Interfaces.Clients;
using Infrastructure.Workers.Api;
using Infrastructure.Workers.Api.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWSLambdas.Api.WorkerHost;

public static class HostExtensions
{
    public static void AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddSingleton<IConfigurationSettings>(new AspNetConfigurationSettings(configuration));
        services.AddSingleton<IHostSettings, HostSettings>();

#if TESTINGONLY
        services.AddSingleton<ICrashReporter>(new NullCrashReporter());
#else
#if HOSTEDONAWS
            services.AddSingleton<ICrashReporter>(c =>
                new AWSCloudWatchCrashReporter(c.Resolve<ILoggerFactory>()));
#endif
#endif

        services.AddSingleton<IRecorder>(c =>
            new CrashTraceOnlyRecorder("Azure API Lambdas", c.Resolve<ILoggerFactory>(),
                c.Resolve<ICrashReporter>()));
        services.AddSingleton<IServiceClient>(c =>
            new InterHostServiceClient(c.Resolve<IHttpClientFactory>(),
                c.Resolve<JsonSerializerOptions>(),
                c.Resolve<IHostSettings>().GetAncillaryApiHostBaseUrl()));
        services.AddSingleton<IQueueMonitoringApiRelayWorker<UsageMessage>, DeliverUsageRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<AuditMessage>, DeliverAuditRelayWorker>();
    }
}