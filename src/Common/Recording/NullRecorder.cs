namespace Common.Recording;

/// <summary>
///     A <see cref="IRecorder" /> that does nothing
/// </summary>
public sealed class NullRecorder : IRecorder
{
    public static readonly IRecorder Instance = new NullRecorder();

    private NullRecorder()
    {
    }

    public void TraceDebug(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
    }

    public void TraceInformation(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceInformation(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
    }

    public void TraceError(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceError(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
    }

    public void TraceWarning(ICallContext? context, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceWarning(ICallContext? context, string messageTemplate, params object[] templateArgs)
    {
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception)
    {
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void Audit(ICallContext? context, string auditCode, string messageTemplate, params object[] templateArgs)
    {
    }

    public void AuditAgainst(ICallContext? context, string againstId, string auditCode, string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void Measure(string eventName, Dictionary<string, object>? additional = null)
    {
    }

    public void TrackUsage(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
    {
    }

    public void TrackUsageFor(ICallContext? context, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
    }
}