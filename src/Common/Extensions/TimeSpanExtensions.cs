using System.Xml;

namespace Common.Extensions;

public static class TimeSpanExtensions
{
    /// <summary>
    ///     Converts the <see cref="value" /> to a TimeSpan value,
    ///     and in the case where the value cannot be converted, uses the <see cref="defaultValue" />
    /// </summary>
    public static TimeSpan ToTimeSpanOrDefault(this string? value, TimeSpan? defaultValue = null)
    {
        if (value.HasNoValue())
        {
            return defaultValue ?? TimeSpan.Zero;
        }

        if (TimeSpan.TryParse(value, out var span))
        {
            return span;
        }

        var iso8601 = ToIso8601TimeSpan(value);
        if (iso8601 != TimeSpan.Zero)
        {
            return iso8601;
        }

        return defaultValue ?? TimeSpan.Zero;
    }

    private static TimeSpan ToIso8601TimeSpan(this string value)
    {
        try
        {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException)
        {
            return TimeSpan.Zero;
        }
    }
}