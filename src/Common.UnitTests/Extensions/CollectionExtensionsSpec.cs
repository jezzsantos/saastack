using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class CollectionExtensionsSpec
{
    [Fact]
    public void WhenContainsIgnoreCaseAndEmptyCollection_ThenReturnsFalse()
    {
        var result = new List<string>().ContainsIgnoreCase("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenContainsIgnoreCaseAndNoMatches_ThenReturnsFalse()
    {
        var result = new List<string>
        {
            "avalue1",
            "avalue2"
        }.ContainsIgnoreCase("anothervalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenContainsIgnoreCaseAndMatches_ThenReturnsTrue()
    {
        var result = new List<string>
        {
            "avalue1",
            "avalue2"
        }.ContainsIgnoreCase("avalue1");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotContainsAndEmpty_ThenReturnsTrue()
    {
        var result = new List<string>().NotContains(item => item == "avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotContainsAndNotMatches_ThenReturnsTrue()
    {
        var result = new List<string>
        {
            "avalue1",
            "avalue2",
            "avalue3"
        }.NotContains(item => item == "avalue9");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotContainsAndMatches_ThenReturnsFalse()
    {
        var result = new List<string>
        {
            "avalue1",
            "avalue2",
            "avalue3"
        }.NotContains(item => item == "avalue2");

        result.Should().BeFalse();
    }
}