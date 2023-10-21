using System.Globalization;

namespace Common.Extensions;

public static class DateTimeExtensions
{
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
    ///     Converts the <see cref="value" /> to UTC and then to
    ///     <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO8601</see>
    /// </summary>
    public static string ToIso8601(this DateTime value)
    {
        var utcDateTime = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        return utcDateTime.ToString("O");
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
    ///     Truncates the <see cref="value" /> to the nearest second.
    /// </summary>
    public static DateTime ToNearestSecond(this DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
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