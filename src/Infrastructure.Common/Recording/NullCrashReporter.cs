using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Recording;
using JetBrains.Annotations;

namespace Infrastructure.Common.Recording;

/// <summary>
///     An <see cref="ICrashReporter" /> that does nothing
/// </summary>
[ExcludeFromCodeCoverage]
public class NullCrashReporter : ICrashReporter
{
    public void Crash(ICallContext? context, CrashLevel level, Exception exception,
        [StructuredMessageTemplate] string messageTemplate, params object[] templateArgs)
    {
    }
}