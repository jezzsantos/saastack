using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class StringExtensionsSpec
{
    [Fact]
    public void WhenHasValueAndNull_ThenReturnsFalse()
    {
        var result = ((string?)null).HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndEmpty_ThenReturnsFalse()
    {
        var result = string.Empty.HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndOnlyWhitespace_ThenReturnsFalse()
    {
        var result = " ".HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndHasValue_ThenReturnsTrue()
    {
        var result = "avalue".HasValue();

        result.Should().BeTrue();
    }
}