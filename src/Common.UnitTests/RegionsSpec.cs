using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class RegionsSpec
{
    [Fact]
    public void WhenGetMemberNameForItemWithNoMemberAttribute_ThenReturnsName()
    {
#if TESTINGONLY
        var result = Region.TestingOnly1.GetDisplayName();

        result.Should().Be("TestingOnly1");
#endif
    }

    [Fact]
    public void WhenGetMemberNameForItemWithMemberAttributeButNoValue_ThenReturnsName()
    {
#if TESTINGONLY
        var result = Region.TestingOnly2.GetDisplayName();

        result.Should().Be("TestingOnly2");
#endif
    }

    [Fact]
    public void WhenGetMemberNameForItemWithMemberAttribute_ThenReturnsAttributeValue()
    {
        var result = Region.AustraliaEast.GetDisplayName();

        result.Should().Be(Regions.AustraliaEast);
    }
}