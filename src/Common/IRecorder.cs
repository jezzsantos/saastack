using Common.Recording;

namespace Common;

/// <summary>
///     Defines a recorder that keeps note of various activities about the system for future examination
/// </summary>
public interface IRecorder
{
    void Audit(ICallContext? context, string auditCode, string messageTemplate, params object[] templateArgs);

    void AuditAgainst(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs);

    void Crash(ICallContext? context, CrashLevel level, Exception exception);

    void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs);

    void Measure(string eventName, Dictionary<string, object>? additional = null);

    void TraceDebug(ICallContext? context, string messageTemplate, params object[] templateArgs);

    void TraceError(ICallContext? context, Exception exception, string messageTemplate, params object[] templateArgs);

    void TraceError(ICallContext? context, string messageTemplate, params object[] templateArgs);

    void TraceInformation(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs);

    void TraceInformation(ICallContext? context, string messageTemplate, params object[] templateArgs);

    void TraceWarning(ICallContext? context, Exception exception, string messageTemplate, params object[] templateArgs);

    void TraceWarning(ICallContext? context, string messageTemplate, params object[] templateArgs);

    void TrackUsage(ICallContext? context, string eventName, Dictionary<string, object>? additional = null);

    void TrackUsageFor(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null);
}