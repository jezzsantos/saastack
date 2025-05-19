using Common.Extensions;
using NodaTime;

namespace Common;

public static class Timezones
{
    //See: https://en.wikipedia.org/wiki/List_of_tz_database_time_zones
    public const string GmtIANA = "GMT";
    public const string NewZealandIANA = "Pacific/Auckland";
    public const string NewZealandWindows = "New Zealand Standard Time";
    public const string SydneyIANA = "Australia/Sydney";
    public const string UniversalCoordinatedIANA = "Etc/UTC";
    public const string UniversalCoordinatedWindows = "UTC";
    public const string UsPacificIANA = "US/Pacific";
    public static readonly TimezoneIANA Sydney = TimezoneIANA.Create(SydneyIANA, TimeSpan.FromHours(10),
        "AEST", TimeSpan.FromHours(11), "AEDT");
    public static readonly TimezoneIANA NewZealand = TimezoneIANA.Create(NewZealandIANA, TimeSpan.FromHours(12),
        "NZST", TimeSpan.FromHours(13), "NZDT");
    public static readonly TimezoneIANA UsPacific = TimezoneIANA.Create(UsPacificIANA, TimeSpan.FromHours(-8),
        "PST", TimeSpan.FromHours(-7), "PDT");
    public static readonly TimezoneIANA Default = UsPacific; //EXTEND: set your default country code
    public static readonly TimezoneIANA Gmt = TimezoneIANA.Create(GmtIANA, TimeSpan.FromHours(0),
        "GMT", TimeSpan.FromHours(0), "GMT");

#if TESTINGONLY
    public static readonly TimezoneIANA Test = TimezoneIANA.Create("testTimezone", TimeSpan.FromHours(1), "TSST",
        TimeSpan.FromHours(1), "TSDT");
#endif

    /// <summary>
    ///     Whether the specified timezone by its <see cref="timezoneId" /> exists
    /// </summary>
    public static bool Exists(string? timezoneId)
    {
        if (timezoneId.NotExists())
        {
            return false;
        }

        return Find(timezoneId).Exists();
    }

    /// <summary>
    ///     Returns the specified timezone by its <see cref="timezoneId" /> if it exists
    /// </summary>
    public static TimezoneIANA? Find(string? timezoneId)
    {
        if (timezoneId.NotExists())
        {
            return null;
        }

#if TESTINGONLY
        if (timezoneId == Test.Id)
        {
            return Test;
        }
#endif
        var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezoneId);
        if (zone.Exists())
        {
            var startOfThisYear =
                Instant.FromDateTimeUtc(DateTime.SpecifyKind(new DateTime(DateTime.UtcNow.Year, 1, 1),
                    DateTimeKind.Utc));
            var endOfYear = startOfThisYear.Plus(Duration.FromDays(365)).Minus(Duration.FromDays(1));

            var intervals = zone.GetZoneIntervals(startOfThisYear, endOfYear)
                .DistinctBy(z => z.Name)
                .ToList();
            var standard = intervals.First(interval => interval.Savings == Offset.Zero);
            var standardOffset = standard.WallOffset.ToTimeSpan();
            var standardCode = standard.Name;
            var daylightSavings = intervals.FirstOrDefault(interval => interval.Savings > Offset.Zero);
            var daylightSavingsOffset = daylightSavings.Exists()
                ? daylightSavings.WallOffset.ToTimeSpan()
                : TimeSpan.Zero;
            var daylightSavingsCode = daylightSavings.Exists()
                ? daylightSavings.Name
                : null;

            return TimezoneIANA.Create(zone.Id, standardOffset, standardCode, daylightSavingsOffset,
                daylightSavingsCode);
        }

        return null;
    }

    /// <summary>
    ///     Returns the specified timezone by its <see cref="timezoneId" /> if it exists,
    ///     or returns <see cref="Default" />
    /// </summary>
    public static TimezoneIANA FindOrDefault(string? timezoneId)
    {
        var exists = Find(timezoneId);
        return exists.Exists()
            ? exists
            : Default;
    }
}

/// <summary>
///     Provides a IANA timezone
/// </summary>
public sealed class TimezoneIANA : IEquatable<TimezoneIANA>
{
    public static TimezoneIANA Create(string id, TimeSpan standardOffset, string standardCode,
        TimeSpan daylightSavingsOffset, string? daylightSavingsCode)
    {
        id.ThrowIfNotValuedParameter(nameof(id));
        standardOffset.ThrowIfInvalidParameter(IsValidOffset, nameof(standardOffset),
            Resources.TimezoneIana_InvalidStandardOffset.Format(standardOffset));
        standardCode.ThrowIfInvalidParameter(IsValidCode, nameof(standardCode),
            Resources.TimezoneIana_InvalidStandardCode.Format(standardCode));
        daylightSavingsOffset.ThrowIfInvalidParameter(IsValidOffset, nameof(daylightSavingsOffset),
            Resources.TimezoneIana_InvalidDaylightSavingsOffset.Format(daylightSavingsOffset));
        if (daylightSavingsCode.Exists())
        {
            daylightSavingsCode.ThrowIfInvalidParameter(IsValidCode,
                nameof(daylightSavingsCode),
                Resources.TimezoneIana_InvalidDaylightSavingsCode.Format(daylightSavingsCode));
        }

        var instance = new TimezoneIANA(id, standardCode, daylightSavingsCode)
        {
            StandardOffset = standardOffset,
            HasDaylightSavings = daylightSavingsCode.Exists(),
            DaylightSavingsOffset = daylightSavingsOffset
        };

        return instance;

        bool IsValidOffset(TimeSpan num)
        {
            return num.TotalHours is >= -14 and <= 14;
        }

        bool IsValidCode(string? code)
        {
            if (code.HasNoValue())
            {
                return false;
            }

            return code.IsMatchWith(@"^[\w]{3,4}$")
                   || code.IsMatchWith(@"^[\+\-]{1}([\d]{2})(:)?([\d]{2})?$");
        }
    }

    private TimezoneIANA(string id, string standardCode, string? daylightSavingsCode)
    {
        id.ThrowIfNotValuedParameter(nameof(id));
        standardCode.ThrowIfNotValuedParameter(nameof(standardCode));
        if (daylightSavingsCode.Exists())
        {
            daylightSavingsCode.ThrowIfNotValuedParameter(nameof(daylightSavingsCode));
        }

        Id = id;
        StandardCode = standardCode;
        DaylightSavingsCode = daylightSavingsCode;
    }

    public string? DaylightSavingsCode { get; private set; }

    public TimeSpan DaylightSavingsOffset { get; private set; }

    public bool HasDaylightSavings { get; private set; }

    public string Id { get; }

    public string StandardCode { get; private set; }

    public TimeSpan StandardOffset { get; private set; }

    public bool Equals(TimezoneIANA? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((TimezoneIANA)obj);
    }

    public override int GetHashCode()
    {
        Id.ThrowIfNotValuedParameter(nameof(Id));

        return Id.GetHashCode();
    }

    public static bool operator ==(TimezoneIANA left, TimezoneIANA right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TimezoneIANA left, TimezoneIANA right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns the <see cref="Id" /> of the timezone
    /// </summary>
    public override string ToString()
    {
        return Id;
    }
}