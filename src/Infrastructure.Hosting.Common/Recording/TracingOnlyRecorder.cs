using System.Text;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Colors = Infrastructure.Common.ConsoleConstants.Colors;

namespace Infrastructure.Hosting.Common.Recording;

/// <summary>
///     Provides a <see cref="IRecorder" /> that only supports tracing
/// </summary>
public class TracingOnlyRecorder : IRecorder
{
    private readonly ILogger _logger;

    public TracingOnlyRecorder(string categoryName, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(categoryName);
        _logger.Log(LogLevel.Debug, Resources.TracingOnlyRecorder_Started);
    }

    public virtual void Audit(ICallContext? call, string auditCode,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        if (auditCode.HasNoValue())
        {
            return;
        }

        var againstId = call.Exists()
            ? call.CallerId
            : null;
        if (againstId.HasValue())
        {
            AuditAgainst(call, againstId, auditCode, messageTemplate, templateArgs);
        }
        else
        {
            TraceInformation(call, $"Audit: {auditCode}, with message: {messageTemplate}", templateArgs);
        }
    }

    public virtual void AuditAgainst(ICallContext? call, string againstId, string auditCode,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        if (auditCode.HasNoValue())
        {
            return;
        }

        TraceInformation(call, $"Audit: {auditCode}, against {againstId}, with message: {messageTemplate}",
            templateArgs);
    }

    public virtual void Crash(ICallContext? call, CrashLevel level, Exception exception)
    {
        Crash(call, level, exception, exception.Message);
    }

    public virtual void Crash(ICallContext? call, CrashLevel level, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        var logLevel = level == CrashLevel.Critical
            ? LogLevel.Critical
            : LogLevel.Error;
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, $"Crash ({level}): {messageTemplate}", templateArgs);
        _logger.Log(logLevel, exception,
            $"{Colors.Reverse}{Colors.Red}{augmentedMessageTemplate}{Colors.Normal}{Colors.NoReverse}",
            augmentedArguments);
    }

    public virtual void Measure(ICallContext? call, string eventName, Dictionary<string, object>? additional = null)
    {
        if (eventName.HasNoValue())
        {
            return;
        }

        TraceInformation(call, $"Measure: {eventName}, with context: {additional.DumpSafely()}");
    }

    public virtual void TraceDebug(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        if (messageTemplate.HasNoValue())
        {
            return;
        }

        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, messageTemplate, templateArgs);
        _logger.LogDebug(augmentedMessageTemplate, augmentedArguments);
    }

    public virtual void TraceError(ICallContext? call, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, messageTemplate, templateArgs);
        _logger.LogError(exception, augmentedMessageTemplate, augmentedArguments);
    }

    public virtual void TraceError(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        if (messageTemplate.HasNoValue())
        {
            return;
        }

        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, messageTemplate, templateArgs);
        _logger.LogError(augmentedMessageTemplate, augmentedArguments);
    }

    public virtual void TraceInformation(ICallContext? call, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, messageTemplate, templateArgs);
        _logger.LogInformation(exception, augmentedMessageTemplate, augmentedArguments);
    }

    public virtual void TraceInformation(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        if (messageTemplate.HasNoValue())
        {
            return;
        }

        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, messageTemplate, templateArgs);
        _logger.LogInformation(augmentedMessageTemplate, augmentedArguments);
    }

    public virtual void TraceWarning(ICallContext? call, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, messageTemplate, templateArgs);
        _logger.LogWarning(exception, augmentedMessageTemplate, augmentedArguments);
    }

    public virtual void TraceWarning(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
        if (messageTemplate.HasNoValue())
        {
            return;
        }

        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(call, messageTemplate, templateArgs);
        _logger.LogWarning(augmentedMessageTemplate, augmentedArguments);
    }

    public virtual void TrackUsage(ICallContext? call, string eventName,
        Dictionary<string, object>? additional = null)
    {
        if (eventName.HasNoValue())
        {
            return;
        }

        var forId = call.Exists()
            ? call.CallerId
            : null;
        if (forId.HasValue())
        {
            TrackUsageFor(call, forId, eventName, additional);
        }
        else
        {
            TraceInformation(call, $"Usage: {eventName}, with context: {additional.DumpSafely()}");
        }
    }

    public virtual void TrackUsageFor(ICallContext? call, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
        if (eventName.HasNoValue())
        {
            return;
        }

        TraceInformation(call, $"Usage: {eventName}, for: {forId}, with context: {additional.DumpSafely()}");
    }

    private static (string MessageTemplate, object[] Arguments) AugmentMessageTemplateAndArguments(
        ICallContext? context, [StructuredMessageTemplate] string messageTemplate, params object[] templateArgs)
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