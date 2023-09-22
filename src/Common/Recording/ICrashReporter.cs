namespace Common.Recording;

/// <summary>
///     Records crash events in the system
/// </summary>
public interface ICrashReporter
{
    void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs);
}