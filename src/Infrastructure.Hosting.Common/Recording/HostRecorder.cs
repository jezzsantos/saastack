using System.Text;
using Application.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces.Services;
using Infrastructure.Common.Recording;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Hosting.Common.Recording;

/// <summary>
///     Provides a <see cref="IRecorder" /> configured for use in a multi-environment Host
/// </summary>
public sealed class HostRecorder : IRecorder, IDisposable
{
    private readonly IAuditReporter _auditReporter;
    private readonly ICrashReporter _crashReporter;
    private readonly ILogger _logger;
    private readonly IMetricReporter _metricsReporter;
    private readonly string _usageComponentName;
    private readonly IUsageReporter _usageReporter;

    public HostRecorder(IDependencyContainer container, ILoggerFactory loggerFactory, HostOptions options) : this(
        container, loggerFactory.CreateLogger(options.HostName), options.Recording)
    {
    }

    internal HostRecorder(ILogger logger, RecorderOptions options, ICrashReporter crashReporter,
        IAuditReporter auditReporter, IMetricReporter metricsReporter, IUsageReporter usageReporter)
    {
        _logger = logger;
        _usageComponentName = options.UsageComponentName;
        _crashReporter = crashReporter;
        _auditReporter = auditReporter;
        _metricsReporter = metricsReporter;
        _usageReporter = usageReporter;
    }

    private HostRecorder(IDependencyContainer container, ILogger logger, RecorderOptions options) : this(logger,
        options, GetCrashReporter(container, logger, options.CurrentEnvironment),
        GetAuditReporter(container, options.CurrentEnvironment),
        GetMetricReporter(container, options.CurrentEnvironment),
        GetUsageReporter(container, options.CurrentEnvironment))
    {
    }

    ~HostRecorder()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            (_auditReporter as IDisposable)?.Dispose();
            // ReSharper disable once SuspiciousTypeConversion.Global
            (_crashReporter as IDisposable)?.Dispose();
            // ReSharper disable once SuspiciousTypeConversion.Global
            (_metricsReporter as IDisposable)?.Dispose();
            // ReSharper disable once SuspiciousTypeConversion.Global
            (_usageReporter as IDisposable)?.Dispose();
        }
    }

    public void TraceDebug(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogDebug(augmentedMessageTemplate, augmentedArguments);
    }

    public void TraceInformation(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogInformation(exception, augmentedMessageTemplate, augmentedArguments);
    }

    public void TraceInformation(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogInformation(augmentedMessageTemplate, augmentedArguments);
    }

    public void TraceWarning(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogWarning(exception, augmentedMessageTemplate, augmentedArguments);
    }

    public void TraceWarning(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogWarning(augmentedMessageTemplate, augmentedArguments);
    }

    public void TraceError(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogError(exception, augmentedMessageTemplate, augmentedArguments);
    }

    public void TraceError(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogError(augmentedMessageTemplate, augmentedArguments);
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception)
    {
        Crash(context, level, exception, exception.Message);
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        var safeContext = context ?? CallContext.CreateUnknown();
        var logLevel = level == CrashLevel.Critical
            ? LogLevel.Critical
            : LogLevel.Error;
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Crash: {messageTemplate}", templateArgs);
        _logger.Log(logLevel, exception, augmentedMessageTemplate, augmentedArguments);

        var errorSourceId = exception.GetBaseException().TargetSite?.ToString();
        if (errorSourceId.Exists())
        {
            _metricsReporter.Measure(safeContext, $"Exceptions: {errorSourceId}");
        }

        _crashReporter.Crash(safeContext, level, exception, messageTemplate, templateArgs);
    }

    public void Audit(ICallContext? context, string auditCode, string messageTemplate, params object[] templateArgs)
    {
        var safeContext = context ?? CallContext.CreateUnknown();
        var againstId = safeContext.CallerId;
        AuditAgainst(safeContext, againstId, auditCode, messageTemplate, templateArgs);
    }

    public void AuditAgainst(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        var safeContext = context ?? CallContext.CreateUnknown();
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Audit: {auditCode}, against {againstId}, {messageTemplate}",
                templateArgs);
        TraceInformation(safeContext, augmentedMessageTemplate, augmentedArguments);
        TrackUsageFor(safeContext, againstId, UsageConstants.Events.UsageScenarios.Audit,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.UsedById, againstId },
                { UsageConstants.Properties.AuditCode, auditCode.ToLowerInvariant() }
            });
        _auditReporter.Audit(safeContext, againstId, auditCode, messageTemplate, templateArgs);
    }

    public void TrackUsage(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        var safeContext = context ?? CallContext.CreateUnknown();
        var forId = safeContext.CallerId;
        TrackUsageFor(safeContext, forId, eventName, additional);
    }

    public void TrackUsageFor(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        ArgumentException.ThrowIfNullOrEmpty(forId);

        var safeContext = context ?? CallContext.CreateUnknown();
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Usage: {eventName}, for {forId}");
        TraceInformation(context, augmentedMessageTemplate, augmentedArguments);

        var properties = additional ?? new Dictionary<string, object>();
        properties.TryAdd(UsageConstants.Properties.Component, _usageComponentName);
        _usageReporter.Track(safeContext, forId, eventName, properties);
    }

    public void Measure(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        var safeContext = context ?? CallContext.CreateUnknown();
        TraceInformation(safeContext, $"Measure: {eventName}");
        var usageContext = additional ?? new Dictionary<string, object>();
        usageContext.Add(UsageConstants.Properties.MetricEventName, eventName.ToLowerInvariant());
        TrackUsage(safeContext, UsageConstants.Events.UsageScenarios.Measurement, usageContext);
        _metricsReporter.Measure(safeContext, eventName, additional ?? new Dictionary<string, object>());
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendFormat("{0}: ", GetType().Name);
        builder.AppendFormat("Crashes-> {0}, ", _crashReporter.GetType().Name);
        builder.AppendFormat("Audits -> {0}, ", _auditReporter.GetType().Name);
        builder.AppendFormat("Usages -> {0}, ", _usageReporter.GetType().Name);
        builder.AppendFormat("Metrics -> {0}", _metricsReporter.GetType().Name);

        return builder.ToString();
    }

    // ReSharper disable once UnusedParameter.Local
    private static ICrashReporter GetCrashReporter(IDependencyContainer container, ILogger logger,
        RecordingEnvironmentOptions options)
    {
        return options.CrashReporting switch
        {
            CrashReporterOption.None => new NullCrashReporter(),
            CrashReporterOption.Cloud =>
#if HOSTEDONAZURE
                new ApplicationInsightsCrashReporter(container),
#elif HOSTEDONAWS
                new AWSCloudWatchCrashReporter(logger),
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(options.MetricReporting))
        };
    }

    private static IAuditReporter GetAuditReporter(IDependencyContainer container, RecordingEnvironmentOptions options)
    {
        return options.AuditReporting switch
        {
            AuditReporterOption.None => new NullAuditReporter(),
            AuditReporterOption.ReliableQueue => new QueuedAuditReporter(container,
                container.ResolveForPlatform<IConfigurationSettings>()),
            _ => throw new ArgumentOutOfRangeException(nameof(options.MetricReporting))
        };
    }

    // ReSharper disable once UnusedParameter.Local
    private static IMetricReporter GetMetricReporter(IDependencyContainer container,
        RecordingEnvironmentOptions options)
    {
        return options.MetricReporting switch
        {
            MetricReporterOption.None => new NullMetricReporter(),
            MetricReporterOption.Cloud =>
#if HOSTEDONAZURE
                new ApplicationInsightsMetricReporter(container),
#elif HOSTEDONAWS
                new AWSCloudWatchMetricReporter(container),
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(options.MetricReporting))
        };
    }

    private static IUsageReporter GetUsageReporter(IDependencyContainer container, RecordingEnvironmentOptions options)
    {
        return options.UsageReporting switch
        {
            UsageReporterOption.None => new NullUsageReporter(),
            UsageReporterOption.ReliableQueue => new QueuedUsageReporter(container,
                container.ResolveForPlatform<IConfigurationSettings>()),
            _ => throw new ArgumentOutOfRangeException(nameof(options.MetricReporting))
        };
    }

    private static (string messageTemplate, object[] arguments) AugmentMessageTemplateAndArguments(
        ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        var arguments = new List<object>(templateArgs);
        var builder = new StringBuilder();

        var unknownCall = CallContext.CreateUnknown();
        var currentCall = context ?? unknownCall;

        builder.Append("Request: {Request}");
        arguments.Insert(0, currentCall.CallId.HasValue()
            ? currentCall.CallId
            : unknownCall.CallId);

        builder.Append(" By: {Caller}");
        arguments.Insert(1, currentCall.CallerId.HasValue()
            ? currentCall.CallerId
            : unknownCall.CallerId);

        if (messageTemplate.HasValue())
        {
            builder.Append($": {messageTemplate}");
        }

        return (builder.ToString(), arguments.ToArray());
    }
}