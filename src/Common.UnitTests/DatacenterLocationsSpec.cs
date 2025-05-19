using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class DatacenterLocationsSpec
{
    [Fact]
    public void WhenFindAndNull_ThenReturnsNull()
    {
        var result = DatacenterLocations.Find(null);

        result.Should().BeNull();
    }

    [Fact]
    public void WhenFindAndUnknown_ThenReturnsNull()
    {
        var result = DatacenterLocations.Find("anunknowncode");

        result!.Should().BeNull();
    }

    [Fact]
    public void WhenFindAndKnown_ThenReturnsKnown()
    {
        var result = DatacenterLocations.Find(DatacenterLocations.AustraliaEastCode);

        result!.Code.Should().Be(DatacenterLocations.AustraliaEastCode);
    }
}