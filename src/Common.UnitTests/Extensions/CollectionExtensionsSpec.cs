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

        result.Should()
            .BeFalse();
    }

    [Fact]
    public void WhenContainsIgnoreCaseAndNoMatches_ThenReturnsFalse()
    {
        var result = new List<string>
        {
            "avalue1",
            "avalue2"
        }.ContainsIgnoreCase("anothervalue");

        result.Should()
            .BeFalse();
    }

    [Fact]
    public void WhenContainsIgnoreCaseAndMatches_ThenReturnsTrue()
    {
        var result = new List<string>
        {
            "avalue1",
            "avalue2"
        }.ContainsIgnoreCase("avalue1");

        result.Should()
            .BeTrue();
    }
}