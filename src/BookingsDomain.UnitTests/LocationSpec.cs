using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace BookingsDomain.UnitTests;

[Trait("Category", "Unit")]
public class LocationSpec
{
    [Fact]
    public void WhenCreateAndEmptyLocation_ThenReturnsError()
    {
        var result = Location.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreate_ThenReturnsLocation()
    {
        var result = Location.Create("alocation").Value;

        result.Name.Should().Be("alocation");
    }
}