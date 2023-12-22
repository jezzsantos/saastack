using Application.Interfaces.Services;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;
using Common.Recording;
using Infrastructure.Common.Recording;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.ApplicationServices;
using Infrastructure.Web.Hosting.Common.Extensions;
using Infrastructure.Workers.Api;
using Infrastructure.Workers.Api.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
#if !TESTINGONLY
using Microsoft.ApplicationInsights;
#endif

namespace AzureFunctions.Api.WorkerHost;

public static class HostExtensions
{
    public static void AddDependencies(this IServiceCollection services, HostBuilderContext context)
    {
        services.AddHttpClient();
        services.AddSingleton<IConfigurationSettings>(new AspNetConfigurationSettings(context.Configuration));
        services.AddSingleton<IApiHostSetting, ApiHostSettings>();

#if TESTINGONLY
        services.AddSingleton<ICrashReporter>(new NullCrashReporter());
#else
#if HOSTEDONAZURE
            //Note: ApplicationInsights TelemetryClient is not added by default by the runtime V4
            services.AddApplicationInsightsTelemetryWorkerService();
            services.AddSingleton<ICrashReporter>(c =>
                new ApplicationInsightsCrashReporter(c.Resolve<TelemetryClient>()));
#endif
#endif

        services.AddSingleton<IRecorder>(c =>
            new CrashTraceOnlyRecorder("Azure API Workers", c.Resolve<ILoggerFactory>(),
                c.Resolve<ICrashReporter>()));
        services.AddSingleton<IServiceClient>(c =>
            new InterHostServiceClient(c.Resolve<IHttpClientFactory>(),
                c.Resolve<IApiHostSetting>().GetAncillaryApiHostBaseUrl()));
        services.AddSingleton<IQueueMonitoringApiRelayWorker<UsageMessage>, DeliverUsageRelayWorker>();
        services.AddSingleton<IQueueMonitoringApiRelayWorker<AuditMessage>, DeliverAuditRelayWorker>();
    }
}