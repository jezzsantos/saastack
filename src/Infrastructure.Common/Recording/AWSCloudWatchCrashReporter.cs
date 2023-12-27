#if HOSTEDONAWS
using Common;
using Common.Extensions;
using Common.Recording;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Provides a <see cref="ICrashReporter" /> that sends its reports to AWS CloudWatch via the configured AWS
///     <see cref="ILogger" />
///     that is already pointing to a CloudWatch log group.
/// </summary>
public class AWSCloudWatchCrashReporter : ICrashReporter
{
    private readonly ILogger _logger;

    public AWSCloudWatchCrashReporter(ILoggerFactory logFactory)
        : this(logFactory.CreateLogger(nameof(AWSCloudWatchCrashReporter)))
    {
    }

    public AWSCloudWatchCrashReporter(ILogger logger)
    {
        _logger = logger;
    }

    public void Crash(ICallContext? context, CrashLevel level, Exception exception, string messageTemplate,
        params object[] templateArgs)
    {
        //Unfortunately the default ILogger does not report a decent stacktrace for exceptions, we need to add it
        var stackTrace = exception.ToString();
        var arguments = templateArgs.HasAny()
            ? templateArgs.Concat(new object[] { stackTrace })
            : new object[] { stackTrace };

        _logger.Log(LogLevel.Critical, exception, $"Crash! {messageTemplate}, details: {{StackTrace}}",
            arguments.ToArray());
    }
}
#endif