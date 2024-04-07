using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Common.Recording;

/// <summary>
///     A <see cref="IRecorder" /> that does nothing
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NoOpRecorder : IRecorder
{
    public static readonly IRecorder Instance = new NoOpRecorder();

    private NoOpRecorder()
    {
    }

    public void TraceDebug(ICallContext? context, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceInformation(ICallContext? context, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceInformation(ICallContext? context, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceError(ICallContext? context, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceError(ICallContext? context, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceWarning(ICallContext? context, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceWarning(ICallContext? context, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception)
    {
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void Audit(ICallContext? context, string auditCode, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void AuditAgainst(ICallContext? context, string againstId, string auditCode,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void Measure(ICallContext? context, string eventName, Dictionary<string, object>? additional = null)
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