using System.Diagnostics;
using Common.Extensions;

namespace Common.Recording;

public static class RecorderExtensions
{
    /// <summary>
    ///     Measure a metric
    /// </summary>
    public static TReturn MeasureWith<TReturn>(this IRecorder recorder, string eventName,
        Func<Dictionary<string, object>, TReturn> action)
    {
        var context = new Dictionary<string, object>();

        var result = action(context);

        recorder.Measure(eventName, context.HasNone()
            ? null
            : context);

        return result;
    }

    /// <summary>
    ///     Measure a metric and how long it took
    /// </summary>
    public static TReturn MeasureWithDuration<TReturn>(this IRecorder recorder, string eventName,
        Func<Dictionary<string, object>, TReturn> action)
    {
        var result = recorder.MeasureWith(eventName, context =>
        {
            var stopwatch = Stopwatch.StartNew();

            var result = action(context);

            stopwatch.Stop();
            context.Add("DurationInMS", $"{stopwatch.Elapsed.TotalMilliseconds:N}");

            return result;
        });

        return result;
    }
}