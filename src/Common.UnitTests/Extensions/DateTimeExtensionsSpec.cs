using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class DateTimeExtensionsSpec
{
    [Fact]
    public void WhenToIso8601WithNull_ThenReturnsEmpty()
    {
        var result = ((DateTime?)null).ToIso8601();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenToIso8601WithLocalTime_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, DateTimeKind.Local);

        var result = time.ToIso8601();

        var offset = TimeZoneInfo.Local.GetUtcOffset(time);
        var offsetTime = time.Subtract(offset);

        result.Should().Be($"2023-09-{offsetTime.Day:D2}T{offsetTime.Hour:D2}:{offsetTime.Minute:D2}:00Z");
    }

    [Fact]
    public void WhenToIso8601WithUniversalTime_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, DateTimeKind.Utc);

        var result = time.ToIso8601();

        result.Should().Be("2023-09-24T12:00:00Z");
    }

    [Fact]
    public void WhenToIso8601WithSeconds_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 59, 0, 0, DateTimeKind.Utc);

        var result = time.ToIso8601();

        result.Should().Be("2023-09-24T12:00:59Z");
    }

    [Fact]
    public void WhenToIso8601WithMilliseconds_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, 99, 0, DateTimeKind.Utc);

        var result = time.ToIso8601();

        result.Should().Be("2023-09-24T12:00:00.099Z");
    }

    [Fact]
    public void WhenToIso8601WithMicroseconds_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, 0, 99, DateTimeKind.Utc);

        var result = time.ToIso8601();

        result.Should().Be("2023-09-24T12:00:00.000099Z");
    }

    [Fact]
    public void WhenToUnixSecondsWithNull_ThenReturnsZero()
    {
        var result = ((DateTime?)null).ToUnixSeconds();

        result.Should().Be(0);
    }

    [Fact]
    public void WhenToUnixSecondsWithLocalTime_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, DateTimeKind.Local);

        var result = time.ToUnixSeconds();

        var offset = TimeZoneInfo.Local.GetUtcOffset(time);
        var offsetTime = time.Subtract(offset);
        var expected = (long)offsetTime.Subtract(DateTime.UnixEpoch)
            .TotalSeconds;

        result.Should().Be(expected);
    }

    [Fact]
    public void WhenToUnixSecondsWithUniversalTime_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, DateTimeKind.Utc);

        var result = time.ToUnixSeconds();

        result.Should().Be(1695556800L);
    }

    [Fact]
    public void WhenToUnixMilliSecondsWithNull_ThenReturnsZero()
    {
        var result = ((DateTime?)null).ToUnixMilliSeconds();

        result.Should().Be(0);
    }

    [Fact]
    public void WhenToUnixMilliSecondsWithLocalTime_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, DateTimeKind.Local);

        var result = time.ToUnixMilliSeconds();

        var offset = TimeZoneInfo.Local.GetUtcOffset(time);
        var offsetTime = time.Subtract(offset);
        var expected = (long)offsetTime.Subtract(DateTime.UnixEpoch)
            .TotalMilliseconds;

        result.Should().Be(expected);
    }

    [Fact]
    public void WhenToUnixMilliSecondsWithUniversalTime_ThenReturnsUtcTime()
    {
        var time = new DateTime(2023, 09, 24, 12, 0, 0, DateTimeKind.Utc);

        var result = time.ToUnixMilliSeconds();

        result.Should().Be(1695556800000L);
    }

    [Fact]
    public void WhenFromIso8601AndNullValue_ThenReturnsMinDate()
    {
        var result = ((string?)null).FromIso8601();

        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenFromIso8601AndEmptyValue_ThenReturnsMinDate()
    {
        var result = string.Empty.FromIso8601();

        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenFromIso8601AndNotISO8601_ThenReturnsMinDate()
    {
        var result = "notadate".FromIso8601();

        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenFromIso8601AndOtherFormat_ThenReturnsMinDate()
    {
        var result = DateTime.UtcNow.ToString("D")
            .FromIso8601();

        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenFromIso8601AndISO8601UtcWithNoMilliseconds_ThenReturnsUtcDate()
    {
        var result = "2023-09-25T12:00:00Z".FromIso8601();

        var expected = new DateTime(2023, 09, 25, 12, 0, 0, DateTimeKind.Utc);
        result.Should().Be(expected);
    }

    [Fact]
    public void WhenFromIso8601AndISO8601UtcWithMillionths_ThenReturnsUtcDate()
    {
        var now = DateTime.UtcNow;
        var result = now.ToIso8601()
            .FromIso8601();

        result.Should().Be(now);
    }

    [Fact]
    public void WhenFromIso8601AndISO8601Local_ThenReturnsUtcDate()
    {
        var result = "2023-09-25T12:00:00+13:00".FromIso8601();

        var expected = new DateTime(2023, 09, 24, 23, 0, 0, DateTimeKind.Utc);
        result.Should().Be(expected);
    }

    [Fact]
    public void WhenFromUnixTimestampWithNull_ThenReturnsMinDate()
    {
        var result = ((long?)null!).FromUnixTimestamp();

        result.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenFromUnixTimestampWithZero_ThenReturnsUnixEpoch()
    {
        var result = 0L.FromUnixTimestamp();

        result.Should().Be(DateTime.UnixEpoch);
    }

    [Fact]
    public void WhenFromUnixTimestampWithPastDateInSeconds_ThenReturnsDate()
    {
        var hundredYearsAgo = DateTime.UtcNow.ToNearestSecond()
            .Subtract(TimeSpan.FromDays(365 * 100));
        var hundredYearsAgoUnixSeconds = hundredYearsAgo.ToUnixSeconds();

        var result = hundredYearsAgoUnixSeconds.FromUnixTimestamp();

        result.Should().Be(hundredYearsAgo);
    }

    [Fact]
    public void WhenFromUnixTimestampWithNowInSeconds_ThenReturnsDate()
    {
        var now = DateTime.UtcNow.ToNearestSecond();
        var nowUnixSeconds = now.ToUnixSeconds();

        var result = nowUnixSeconds.FromUnixTimestamp();

        result.Should().Be(now);
    }

    [Fact]
    public void WhenFromUnixTimestampWithFutureDateInSeconds_ThenReturnsDate()
    {
        var tenYearsAFromNow = DateTime.UtcNow.ToNearestSecond()
            .Add(TimeSpan.FromDays(365 * 10));
        var tenYearsFromNowUnixSeconds = tenYearsAFromNow.ToUnixSeconds();

        var result = tenYearsFromNowUnixSeconds.FromUnixTimestamp();

        result.Should().Be(tenYearsAFromNow);
    }

    [Fact]
    public void WhenHasValueAndMinValue_ThenReturnsFalse()
    {
        var result = DateTime.MinValue.HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndMinValueToUtc_ThenReturnsFalse()
    {
        var result = DateTime.MinValue.ToUniversalTime().HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndMinValueToLocal_ThenReturnsFalse()
    {
        var result = DateTime.MinValue.ToLocalTime().HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndUtcMinValue_ThenReturnsFalse()
    {
        var result = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc).HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndLocalMinValue_ThenReturnsFalse()
    {
        var result = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Local).HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndUnspecifiedMinValue_ThenReturnsFalse()
    {
        var result = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified).HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndNotMinValue_ThenReturnsTrue()
    {
        var result = DateTime.UtcNow.HasValue();

        result.Should().BeTrue();
    }
}