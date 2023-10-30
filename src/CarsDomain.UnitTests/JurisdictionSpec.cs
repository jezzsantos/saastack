using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class JurisdictionSpec
{
    [Fact]
    public void WhenCreateAndEmptyJurisdiction_ThenReturnsError()
    {
        var result = Jurisdiction.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateAndUnknownJurisdiction_ThenReturnsError()
    {
        var result = Jurisdiction.Create("anunknownjurisdiction");

        result.Should().BeError(ErrorCode.Validation, Resources.Jurisdiction_UnknownJurisdiction);
    }

    [Fact]
    public void WhenCreate_ThenReturnsJurisdiction()
    {
        var result = Jurisdiction.Create(Jurisdiction.AllowedCountries[0]).Value;

        result.Name.Should().Be(Jurisdiction.AllowedCountries[0]);
    }
}