using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class TimeSpanExtensionsSpec
{
    [Fact]
    public void WhenToTimeSpanOrDefaultWithNullValue_ThenReturnsZero()
    {
        var result = ((string?)null).ToTimeSpanOrDefault();

        result.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void WhenToTimeSpanOrDefaultWithNonZeroValue_ThenReturnsValue()
    {
        var result = "PT1H".ToTimeSpanOrDefault();

        result.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void WhenToTimeSpanOrDefaultWithOtherValue_ThenReturnsZero()
    {
        var span = TimeSpan.FromDays(1);

        var result = span.ToString().ToTimeSpanOrDefault();

        result.Should().Be(span);
    }

    [Fact]
    public void WhenToTimeSpanOrDefaultWithNullValueAndDefaultValue_ThenReturnsDefaultValue()
    {
        var defaultValue = TimeSpan.FromHours(1);

        var result = ((string?)null).ToTimeSpanOrDefault(defaultValue);

        result.Should().Be(defaultValue);
    }

    [Fact]
    public void WhenToTimeSpanOrDefaultWithInvalidTimeSpanValue_ThenReturnsZero()
    {
        var result = "notavalidtimespan".ToTimeSpanOrDefault();

        result.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void WhenToTimeSpanOrDefaultWithInvalidTimeSpanValueAndDefaultValue_ThenReturnsDefaultValue()
    {
        var defaultValue = TimeSpan.FromHours(1);

        var result = "notavalidtimespan".ToTimeSpanOrDefault(defaultValue);

        result.Should().Be(defaultValue);
    }

    [Fact]
    public void WhenToTimeSpanOrDefaultWithStringSerializedSpan_ThenReturnsTimeSpan()
    {
        var span = TimeSpan.FromHours(1);

        var result = span.ToString().ToTimeSpanOrDefault();

        result.Should().Be(span);
    }
}