using System.Globalization;

namespace Common.Extensions;

public static class DateTimeExtensions
{
    private static readonly TimeSpan DefaultIsNearRange = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Converts the <see cref="value" /> to a UTC date,
    ///     but only if the <see cref="value" /> is in the
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see> format.
    /// </summary>
    public static DateTime FromIso8601(this string? value)
    {
        if (value.HasNoValue())
        {
            return default;
        }

        var supportedIsoFormats = new[]
        {
            "yyyyMMddTHHmmssZ", "yyyyMMddTHHmmsszz", "yyyyMMddTHHmmsszzz",
            "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:sszz", "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:ss.FZ", "yyyy-MM-ddTHH:mm:ss.FFZ", "yyyy-MM-ddTHH:mm:ss.FFFZ",
            "yyyy-MM-ddTHH:mm:ss.FFFFZ", "yyyy-MM-ddTHH:mm:ss.FFFFFZ", "yyyy-MM-ddTHH:mm:ss.FFFFFFZ",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ"
        };
        if (DateTime.TryParseExact(value, supportedIsoFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None,
                out var date))
        {
            return date.Kind == DateTimeKind.Utc
                ? date
                : date.ToUniversalTime();
        }

        return default;
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a UTC date only,
    ///     but only if the <see cref="value" /> is in the
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see> format.
    /// </summary>
    public static DateOnly FromIso8601DateOnly(this string? value)
    {
        if (value.HasNoValue())
        {
            return DateOnly.MinValue;
        }

        var dateOnly = DateOnly.Parse(value);
        return dateOnly.HasValue()
            ? dateOnly
            : DateOnly.MinValue;
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a UTC date,
    ///     but only if the <see cref="value" /> is in the UNIX Timestamp format.
    /// </summary>
    public static DateTime FromUnixTimestamp(this long? value)
    {
        if (value is null)
        {
            return default;
        }

        return value.FromUnixTimestamp();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a UTC date,
    ///     but only if the <see cref="value" /> is in the UNIX Timestamp format (in secs).
    /// </summary>
    public static DateTime FromUnixTimestamp(this long value)
    {
        return DateTime.UnixEpoch.AddSeconds(value);
    }

    /// <summary>
    ///     Whether the specified <see cref="value" /> is not the default value of <see cref="DateTime.MinValue" />
    /// </summary>
    public static bool HasValue(this DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
        {
            return value != DateTime.MinValue.ToLocalTime()
                   && value != DateTime.MinValue;
        }

        return value != DateTime.MinValue;
    }

    /// <summary>
    ///     Whether the specified <see cref="value" /> is not the default value of <see cref="DateTime.MinValue" />
    /// </summary>
    public static bool HasValue(this DateTime? value)
    {
        if (!value.HasValue)
        {
            return false;
        }

        return value.Value.HasValue();
    }

    /// <summary>
    ///     Whether the specified <see cref="value" /> is not the default value of <see cref="DateTime.MinValue" />
    /// </summary>
    public static bool HasValue(this DateOnly value)
    {
        return value != DateOnly.MinValue;
    }

    /// <summary>
    ///     Whether the specified <see cref="value" /> is not the default value of <see cref="DateTime.MinValue" />
    /// </summary>
    public static bool HasValue(this DateOnly? value)
    {
        if (!value.HasValue)
        {
            return false;
        }

        return value.Value.HasValue();
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is after the <see cref="other" />
    /// </summary>
    public static bool IsAfter(this DateTime value, DateTime other)
    {
        return value > other;
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is before the <see cref="other" />
    /// </summary>
    public static bool IsBefore(this DateTime value, DateTime other)
    {
        return value < other;
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is near enough to the <see cref="other" />
    /// </summary>
    public static bool IsNear(this DateTime value, DateTime other)
    {
        if (value.Equals(other))
        {
            return true;
        }

        return value.IsNear(other, DefaultIsNearRange);
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is near enough to the <see cref="other" />
    ///     within the specified <see cref="rangeInMilliseconds" />
    /// </summary>
    public static bool IsNear(this DateTime value, DateTime other, int rangeInMilliseconds)
    {
        if (value.Equals(other))
        {
            return true;
        }

        return value.AddMilliseconds(rangeInMilliseconds) >= other
               && value.AddMilliseconds(-rangeInMilliseconds) <= other;
    }

    /// <summary>
    ///     Whether the <see cref="value" /> is near enough to the <see cref="other" />
    ///     within the specified <see cref="range" />
    /// </summary>
    public static bool IsNear(this DateTime value, DateTime other, TimeSpan range)
    {
        return value.IsNear(other, (int)range.TotalMilliseconds);
    }

    /// <summary>
    ///     Subtracts the <see cref="days" /> from the <see cref="value" />
    /// </summary>
    public static DateTime SubtractDays(this DateTime value, int days)
    {
        return value.AddDays(-days);
    }

    /// <summary>
    ///     Subtracts the <see cref="hours" /> from the <see cref="value" />
    /// </summary>
    public static DateTime SubtractHours(this DateTime value, int hours)
    {
        return value.AddHours(-hours);
    }

    /// <summary>
    ///     Subtracts the <see cref="seconds" /> from the <see cref="value" />
    /// </summary>
    public static DateTime SubtractSeconds(this DateTime value, int seconds)
    {
        return value.AddSeconds(-seconds);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see>
    /// </summary>
    public static string ToIso8601(this DateTime value)
    {
        var utcDateTime = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        // Note: we are using the custom format, instead of using the built-in formatter "O", because we don't want any trailing zeros before the 'Z' character
        return utcDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK");
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see>
    /// </summary>
    public static string ToIso8601(this DateTime? value)
    {
        if (!value.HasValue)
        {
            return string.Empty;
        }

        return value.Value.ToIso8601();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see>
    /// </summary>
    public static string? ToIso8601(this DateOnly? date)
    {
        return date?.ToIso8601();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see>
    /// </summary>
    public static string ToIso8601(this DateOnly date)
    {
        return date.ToString("O");
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see>
    /// </summary>
    public static string ToIso8601(this DateTimeOffset value)
    {
        var utcDateTime = value.ToUniversalTime();

        return utcDateTime.ToString("O");
    }

    /// <summary>
    ///     Truncates the <see cref="value" /> to the nearest minute.
    /// </summary>
    public static DateTime ToNearestMinute(this DateTime value)
    {
        var microsecondOffset = TimeSpan.FromSeconds(value.Second)
            .Add(TimeSpan.FromMilliseconds(value.Millisecond))
            .Add(TimeSpan.FromMicroseconds(value.Microsecond));
        var nanosecondsInTicks = value.Nanosecond != 0
            ? value.Nanosecond / 100
            : 0;
        return value.Subtract(microsecondOffset).AddTicks(-nanosecondsInTicks);
    }

    /// <summary>
    ///     Truncates the <see cref="value" /> to the nearest second.
    /// </summary>
    public static DateTime ToNearestSecond(this DateTime value)
    {
        var microsecondOffset = TimeSpan.FromMilliseconds(value.Millisecond)
            .Add(TimeSpan.FromMicroseconds(value.Microsecond));
        var nanosecondsInTicks = value.Nanosecond != 0
            ? value.Nanosecond / 100
            : 0;
        return value.Subtract(microsecondOffset).AddTicks(-nanosecondsInTicks);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to a UNIX timestamp in milliseconds
    /// </summary>
    public static long ToUnixMilliSeconds(this DateTime value)
    {
        var utcDateTime = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        return (long)utcDateTime.Subtract(DateTime.UnixEpoch)
            .TotalMilliseconds;
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to a UNIX timestamp in milliseconds
    /// </summary>
    public static long ToUnixMilliSeconds(this DateTime? value)
    {
        if (!value.HasValue)
        {
            return 0;
        }

        return value.Value.ToUnixMilliSeconds();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to a UNIX timestamp in seconds
    /// </summary>
    public static long ToUnixSeconds(this DateTime value)
    {
        var utcDateTime = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        return utcDateTime.Subtract(DateTime.UnixEpoch)
            .Ticks / TimeSpan.TicksPerSecond;
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to UTC and then to a UNIX timestamp in seconds
    /// </summary>
    public static long ToUnixSeconds(this DateTime? value)
    {
        if (!value.HasValue)
        {
            return 0;
        }

        return value.Value.ToUnixSeconds();
    }
}