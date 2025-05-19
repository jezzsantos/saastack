using JetBrains.Annotations;

namespace Common.Recording;

/// <summary>
///     Records crash events in the system
/// </summary>
public interface ICrashReporter
{
    void Crash(ICallContext? call, CrashLevel level, Exception exception,
        [StructuredMessageTemplate] string messageTemplate, params object[] templateArgs);
}