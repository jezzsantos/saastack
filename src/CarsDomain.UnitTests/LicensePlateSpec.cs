using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class LicensePlateSpec
{
    [Fact]
    public void WhenCreateAndUnknownJurisdiction_ThenReturnsError()
    {
        var result = LicensePlate.Create("anunknownjurisdiction", "aplate");

        result.Should().BeError(ErrorCode.Validation, Resources.Jurisdiction_UnknownJurisdiction);
    }

    [Fact]
    public void WhenCreateAndInvalidPlate_ThenReturnsError()
    {
        var result = LicensePlate.Create(Jurisdiction.AllowedCountries[0], "^invalid^");

        result.Should().BeError(ErrorCode.Validation, Resources.NumberPlate_InvalidNumberPlate);
    }

    [Fact]
    public void WhenCreate_ThenReturnsError()
    {
        var result = LicensePlate.Create(Jurisdiction.AllowedCountries[0], "aplate").Value;

        result.Jurisdiction.Name.Should().Be(Jurisdiction.AllowedCountries[0]);
        result.Number.Registration.Should().Be("aplate");
    }
}