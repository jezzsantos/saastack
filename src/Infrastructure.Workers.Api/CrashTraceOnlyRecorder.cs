using Common;
using Common.Recording;
using Infrastructure.Hosting.Common.Recording;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Workers.Api;

/// <summary>
///     Provides a <see cref="IRecorder" /> that only traces and crashes, and traces everything else,
///     because we don't want to be producing from this recorder that could be relayed by this process,
///     since that could create a infinite cycle.
/// </summary>
public sealed class CrashTraceOnlyRecorder : TracingOnlyRecorder
{
    private readonly ICrashReporter _crasher;

    public CrashTraceOnlyRecorder(string categoryName, ILoggerFactory loggerFactory, ICrashReporter crashReporter) :
        base(categoryName, loggerFactory)
    {
        _crasher = crashReporter;
    }

    public override void Crash(ICallContext? call, CrashLevel level, Exception exception)
    {
        base.Crash(call, level, exception);
        _crasher.Crash(call, level, exception, string.Empty);
    }

    public override void Crash(ICallContext? call, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        base.Crash(call, level, exception, messageTemplate);
        _crasher.Crash(call, level, exception, messageTemplate, templateArgs);
    }
}