using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class VehicleOwnerSpec
{
    [Fact]
    public void WhenCreateWithEmptyOwnerId_ThenReturnsError()
    {
        var result = VehicleOwner.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreate_ThenReturnsOwner()
    {
        var owner = VehicleOwner.Create("anownerid").Value;

        owner.OwnerId.Should().Be("anownerid");
    }
}