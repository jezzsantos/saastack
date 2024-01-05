using FluentAssertions;
using NodaTime;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class TimezonesSpec
{
    [Fact]
    public void WhenExistsAndUnknown_ThenReturnsFalse()
    {
        var result = Timezones.Exists("notatimezone");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenExistsById_ThenReturnsTrue()
    {
        var result = Timezones.Exists(Timezones.Default.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFindTimeZoneAndUnknown_ThenReturnsNull()
    {
        var result = Timezones.Find("notatimezone");

        result.Should().BeNull();
    }

    [Fact]
    public void WhenFindForNewZealand_ThenReturnsZone()
    {
        var result = Timezones.Find(Timezones.NewZealandIANA);

        result!.Id.Should().Be(Timezones.NewZealandIANA);
        result.StandardCode.Should().Be("NZST");
        result.StandardOffset.Should().Be(TimeSpan.FromHours(12));
        result.HasDaylightSavings.Should().BeTrue();
        result.DaylightSavingsCode.Should().Be("NZDT");
        result.DaylightSavingsOffset.Should().Be(TimeSpan.FromHours(13));
    }

    [Fact]
    public void WhenFindForNonDaylightSavingsTimezone_ThenReturnsZone()
    {
        var result = Timezones.Find("Pacific/Honolulu");

        result!.Id.Should().Be("Pacific/Honolulu");
        result.StandardCode.Should().Be("HST");
        result.StandardOffset.Should().Be(TimeSpan.FromHours(-10));
        result.HasDaylightSavings.Should().BeFalse();
        result.DaylightSavingsCode.Should().BeNull();
        result.DaylightSavingsOffset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void WhenFindForEveryCountryCode_ThenReturnsCode()
    {
        var timezones = DateTimeZoneProviders.Tzdb.Ids;
        foreach (var timezone in timezones)
        {
            var result = Timezones.Find(timezone);

            result.Should().NotBeNull($"{timezone} should have been found by Id");
        }
    }
}