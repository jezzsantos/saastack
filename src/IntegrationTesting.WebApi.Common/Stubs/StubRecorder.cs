using Common;
using Common.Recording;

namespace IntegrationTesting.WebApi.Common.Stubs;

/// <summary>
///     Provides a stub for testing <see cref="IRecorder" />
/// </summary>
public class StubRecorder : IRecorder
{
    public string? LastAuditAuditCode { get; private set; }

    public object[]? LastCrashArguments { get; private set; }

    public Exception? LastCrashException { get; private set; }

    public CrashLevel? LastCrashLevel { get; private set; }

    public string? LastCrashMessageTemplate { get; private set; }

    public Dictionary<string, object>? LastMeasureAdditional { get; private set; }

    public string? LastMeasureEventName { get; private set; }

    public object[]? LastTraceArguments { get; private set; }

    public StubRecorderTraceLevel? LastTraceLevel { get; private set; }

    public string? LastTraceMessageTemplate { get; private set; }

    public Dictionary<string, object>? LastUsageAdditional { get; private set; }

    public string? LastUsageEventName { get; private set; }

    public void TraceDebug(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = StubRecorderTraceLevel.Debug;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
    }

    public void TraceInformation(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastTraceLevel = StubRecorderTraceLevel.Information;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
    }

    public void TraceInformation(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = StubRecorderTraceLevel.Information;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
    }

    public void TraceWarning(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastTraceLevel = StubRecorderTraceLevel.Warning;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
    }

    public void TraceWarning(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = StubRecorderTraceLevel.Warning;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
    }

    public void TraceError(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastTraceLevel = StubRecorderTraceLevel.Error;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
    }

    public void TraceError(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
        LastTraceLevel = StubRecorderTraceLevel.Error;
        LastTraceMessageTemplate = messageTemplate;
        LastTraceArguments = templateArgs;
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception)
    {
        LastCrashLevel = level;
        LastCrashException = exception;
        LastCrashMessageTemplate = null;
        LastCrashArguments = null;
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        LastCrashLevel = level;
        LastCrashException = exception;
        LastCrashMessageTemplate = messageTemplate;
        LastCrashArguments = templateArgs;
    }

    public void Audit(ICallContext? context, string auditCode, string messageTemplate, params object[] templateArgs)
    {
        LastAuditAuditCode = auditCode;
    }

    public void AuditAgainst(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
        LastAuditAuditCode = auditCode;
    }

    public void TrackUsage(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        LastUsageEventName = eventName;
        LastUsageAdditional = additional;
    }

    public void TrackUsageFor(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
        LastUsageEventName = eventName;
        LastUsageAdditional = additional;
    }

    public void Measure(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
        LastMeasureEventName = eventName;
        LastMeasureAdditional = additional;
    }

    public void Reset()
    {
        LastTraceLevel = null;
        LastTraceMessageTemplate = null;
        LastTraceArguments = null;
        LastCrashLevel = null;
        LastCrashException = null;
        LastCrashMessageTemplate = null;
        LastCrashArguments = null;
        LastMeasureEventName = null;
        LastMeasureAdditional = null;
        LastUsageEventName = null;
        LastUsageAdditional = null;
        LastAuditAuditCode = null;
    }
}

/// <summary>
///     Defines the trace level of the recorder
/// </summary>
public enum StubRecorderTraceLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3
}