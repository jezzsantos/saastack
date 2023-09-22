using Common;
using Common.Recording;

namespace Infrastructure.Common.Recording;

/// <summary>
///     An <see cref="ICrashReporter" /> that does nothing
/// </summary>
public class NullCrashReporter : ICrashReporter
{
    public void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
    }
}