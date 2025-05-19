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

    public void Audit(ICallContext? call, string auditCode, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void AuditAgainst(ICallContext? call, string againstId, string auditCode,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void Crash(ICallContext? call, CrashLevel level, Exception exception)
    {
    }

    public void Crash(ICallContext? call, CrashLevel level, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void Measure(ICallContext? call, string eventName, Dictionary<string, object>? additional = null)
    {
    }

    public void TraceDebug(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceError(ICallContext? call, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceError(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceInformation(ICallContext? call, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceInformation(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceWarning(ICallContext? call, Exception exception,
        [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TraceWarning(ICallContext? call, [StructuredMessageTemplate] string messageTemplate,
        params object[] templateArgs)
    {
    }

    public void TrackUsage(ICallContext? call, string eventName, Dictionary<string, object>? additional = null)
    {
    }

    public void TrackUsageFor(ICallContext? call, string forId, string eventName,
        Dictionary<string, object>? additional = null)
    {
    }
}