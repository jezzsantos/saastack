using System.Text;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Provides a <see cref="IRecorder" /> that only supports tracing
/// </summary>
public sealed class TracingOnlyRecorder : IRecorder
{
    private readonly ILogger _logger;

    public TracingOnlyRecorder(string categoryName, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(categoryName);
        _logger.Log(LogLevel.Debug, Resources.TracingOnlyRecorder_Started);
    }

    public void Audit(ICallContext? context, string auditCode, string messageTemplate, params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Audit: {messageTemplate}", templateArgs);
        TraceInformation(context, augmentedMessageTemplate, augmentedArguments);
    }

    public void AuditAgainst(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Audit, for '{againstId}': {messageTemplate}", templateArgs);
        TraceInformation(context, augmentedMessageTemplate, augmentedArguments);
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception)
    {
        TraceError(context, exception, "Crash");
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Crash: {messageTemplate}", templateArgs);
        TraceError(context, exception, augmentedMessageTemplate, augmentedArguments);
    }

    public void Measure(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        var additionalAsJson = additional.Exists()
            ? additional.ToJson()!
            : string.Empty;
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Measure, '{eventName}':", additionalAsJson.ToArray());
        TraceInformation(context, augmentedMessageTemplate, augmentedArguments);
    }

    public void TraceDebug(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
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

    public void TrackUsage(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        var additionalAsJson = additional.Exists()
            ? additional.ToJson()!
            : string.Empty;
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Usage, '{eventName}':", additionalAsJson.ToArray());
        TraceInformation(context, augmentedMessageTemplate, augmentedArguments);
    }

    public void TrackUsageFor(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
        var additionalAsJson = additional.Exists()
            ? additional.ToJson()!
            : string.Empty;
        var (augmentedMessageTemplate, augmentedArguments) =
            AugmentMessageTemplateAndArguments(context, $"Usage, '{eventName}', for '{forId}':",
                additionalAsJson.ToArray());
        TraceInformation(context, augmentedMessageTemplate, augmentedArguments);
    }

    private static (string MessageTemplate, object[] Arguments) AugmentMessageTemplateAndArguments(
        ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        var arguments = new List<object>(templateArgs);
        var builder = new StringBuilder();
        if (context.Exists())
        {
            builder.Append("Request: {Request} ");
            arguments.Insert(0, context.CallId.HasValue()
                ? context.CallId
                : "Uncorrelated");

            var isAuthenticatedCaller =
                context.CallerId.HasValue() && context.CallerId != CallerConstants.AnonymousUserId;
            builder.Append(isAuthenticatedCaller
                ? "(by {Caller}) "
                : "(anonymously) ");
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