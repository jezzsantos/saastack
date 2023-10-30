using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class NumberPlateSpec
{
    [Fact]
    public void WhenCreateAndEmptyNumber_ThenReturnsError()
    {
        var result = NumberPlate.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateAndInvalidNumber_ThenReturnsError()
    {
        var result = NumberPlate.Create("^invalid^");

        result.Should().BeError(ErrorCode.Validation, Resources.NumberPlate_InvalidNumberPlate);
    }

    [Fact]
    public void WhenCreate_ThenReturnsPlate()
    {
        var result = NumberPlate.Create("aplate").Value;

        result.Registration.Should().Be("aplate");
    }
}