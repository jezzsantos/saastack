using System.Text;
using Application.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces;
using Domain.Interfaces.Services;
using Infrastructure.Common.Recording;
using Microsoft.Extensions.Logging;
using Colors = Infrastructure.Common.ConsoleConstants.Colors;

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

    public void Audit(ICallContext? context, string auditCode, string messageTemplate, params object[] templateArgs)
    {
        if (auditCode.HasNoValue())
        {
            return;
        }

        var againstId = context.Exists()
            ? context.CallerId
            : null;
        if (againstId.HasValue())
        {
            AuditAgainst(context, againstId, auditCode, messageTemplate, templateArgs);
        }
        else
        {
            TraceInformation(context, $"Audit: {auditCode}, with message: {messageTemplate}", templateArgs);
        }
    }

    public void AuditAgainst(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        if (auditCode.HasNoValue())
        {
            return;
        }

        TraceInformation(context, $"Audit: {auditCode}, against: {againstId}, with message: {messageTemplate}",
            templateArgs);
        TrackUsageFor(context, againstId, UsageConstants.Events.UsageScenarios.Generic.Audit,
            new Dictionary<string, object>
            {
                { UsageConstants.Properties.UsedById, againstId },
                { UsageConstants.Properties.AuditCode, auditCode.ToLowerInvariant() }
            });
        _auditReporter.Audit(context, againstId, auditCode, messageTemplate, templateArgs);
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception)
    {
        Crash(context, level, exception, exception.Message);
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        var logLevel = level == CrashLevel.Critical
            ? LogLevel.Critical
            : LogLevel.Error;
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Crash ({level}): {messageTemplate}", templateArgs);
        _logger.Log(logLevel, exception,
            $"{Colors.Reverse}{Colors.Red}{augmentedMessageTemplate}{Colors.Normal}{Colors.NoReverse}",
            augmentedArguments);

        var errorSourceId = exception.GetBaseException().TargetSite?.ToString();
        if (errorSourceId.Exists())
        {
            _metricsReporter.Measure(context, $"Exceptions: {errorSourceId}");
        }

        _crashReporter.Crash(context, level, exception, messageTemplate, templateArgs);
    }

    public void Measure(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        if (eventName.HasNoValue())
        {
            return;
        }

        TraceInformation(context, $"Measure: {eventName}, with context: {additional.DumpSafely()}");

        var usageContext = additional ?? new Dictionary<string, object>();
        usageContext.Add(UsageConstants.Properties.MetricEventName, eventName.ToLowerInvariant());
        TrackUsage(context, UsageConstants.Events.UsageScenarios.Generic.Measurement, usageContext);
        _metricsReporter.Measure(context, eventName, additional ?? new Dictionary<string, object>());
    }

    public void TraceDebug(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        if (messageTemplate.HasNoValue())
        {
            return;
        }

        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogDebug(augmentedMessageTemplate, augmentedArguments);
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
        if (messageTemplate.HasNoValue())
        {
            return;
        }

        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogError(augmentedMessageTemplate, augmentedArguments);
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
        if (messageTemplate.HasNoValue())
        {
            return;
        }

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
        if (messageTemplate.HasNoValue())
        {
            return;
        }

        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, messageTemplate, templateArgs);
        _logger.LogWarning(augmentedMessageTemplate, augmentedArguments);
    }

    public void TrackUsage(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        if (eventName.HasNoValue())
        {
            return;
        }

        var forId = context.Exists()
            ? context.CallerId
            : null;
        if (forId.HasValue())
        {
            TrackUsageFor(context, forId, eventName, additional);
        }
        else
        {
            TraceInformation(context, $"Usage: {eventName}, with context: {additional.DumpSafely()}");
        }
    }

    public void TrackUsageFor(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
        if (eventName.HasNoValue())
        {
            return;
        }

        TraceInformation(context, $"Usage: {eventName}, for: {forId}, with context: {additional.DumpSafely()}");

        var properties = additional ?? new Dictionary<string, object>();
        properties.TryAdd(UsageConstants.Properties.Component, _usageComponentName);
        _usageReporter.TrackAsync(context, forId, eventName, properties).GetAwaiter().GetResult();
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
            CrashReporterOption.None => new NoOpCrashReporter(),
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
            AuditReporterOption.None => new NoOpAuditReporter(),
            AuditReporterOption.ReliableQueue => new QueuedAuditReporter(container,
                container.GetRequiredServiceForPlatform<IConfigurationSettings>()),
            _ => throw new ArgumentOutOfRangeException(nameof(options.MetricReporting))
        };
    }

    // ReSharper disable once UnusedParameter.Local
    private static IMetricReporter GetMetricReporter(IDependencyContainer container,
        RecordingEnvironmentOptions options)
    {
        return options.MetricReporting switch
        {
            MetricReporterOption.None => new NoOpMetricReporter(),
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
            UsageReporterOption.None => new NoOpUsageReporter(),
            UsageReporterOption.ReliableQueue => new QueuedUsageReporter(container,
                container.GetRequiredServiceForPlatform<IConfigurationSettings>()),
            _ => throw new ArgumentOutOfRangeException(nameof(options.MetricReporting))
        };
    }

    private static (string messageTemplate, object[] arguments) AugmentMessageTemplateAndArguments(
        ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        var arguments = new List<object>(templateArgs);
        var builder = new StringBuilder();
        if (context.Exists())
        {
            builder.Append("Request: {Request} ");
            arguments.Insert(0, context.CallId.HasValue()
                ? $"{context.CallId},"
                : $"{CallConstants.UncorrelatedCallId},");

            var isAuthenticatedCaller =
                context.CallerId.HasValue() && context.CallerId != CallerConstants.AnonymousUserId;
            builder.Append(isAuthenticatedCaller
                ? "(by {Caller}) "
                : "(by anonymous) ");
            if (isAuthenticatedCaller)
            {
                arguments.Insert(1, context.CallerId);
            }

            builder.Append(' ');
        }

        builder.Append(messageTemplate);

        return (builder.ToString(), arguments.ToArray());
    }
}

internal static class RecorderExtensions
{
    public static string DumpSafely(this Dictionary<string, object>? additional)
    {
        if (additional.NotExists() || additional.HasNone())
        {
            return "none";
        }

        var builder = new StringBuilder();
        builder.Append("<");
        var counter = -1;
        foreach (var (key, value) in additional)
        {
            if (++counter > 0)
            {
                builder.Append(", ");
            }

            var safeValue = value.Exists()
                ? value.ToString() ?? string.Empty
                : string.Empty;
            var escapedValue = safeValue
                .Replace("{", @"{{").Replace("}", @"}}");
            builder.Append($"\"{key}\": {escapedValue}");
        }

        builder.Append(">");

        return builder.ToString();
    }
}