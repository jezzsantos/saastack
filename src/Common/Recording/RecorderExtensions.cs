using System.Diagnostics;
using Common.Extensions;

namespace Common.Recording;

public static class RecorderExtensions
{
    /// <summary>
    ///     Measure a metric
    /// </summary>
    public static TReturn MeasureWith<TReturn>(this IRecorder recorder, ICallContext? context, string eventName,
        Func<Dictionary<string, object>, TReturn> action)
    {
        var additional = new Dictionary<string, object>();

        var result = action(additional);

        recorder.Measure(context, eventName, additional.HasNone()
            ? null
            : additional);

        return result;
    }

    /// <summary>
    ///     Measure a metric and how long it took
    /// </summary>
    public static TReturn MeasureWithDuration<TReturn>(this IRecorder recorder, ICallContext? context, string eventName,
        Func<Dictionary<string, object>, TReturn> action)
    {
        var result = recorder.MeasureWith(context, eventName, additional =>
        {
            var stopwatch = Stopwatch.StartNew();

            var result = action(additional);

            stopwatch.Stop();
            additional.Add("DurationInMS", $"{stopwatch.Elapsed.TotalMilliseconds:N}");

            return result;
        });

        return result;
    }
}