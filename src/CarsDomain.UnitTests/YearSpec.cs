using Common;
using Common.Extensions;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class YearSpec
{
    [Fact]
    public void WhenCreateAndZeroYear_ThenReturnsError()
    {
        var result = Year.Create(0);

        result.Should().BeError(ErrorCode.Validation, Resources.Year_InvalidNumber.Format(Year.MinYear, Year.MaxYear));
    }

    [Fact]
    public void WhenCreateAndYearLessThanMin_ThenReturnsError()
    {
        var result = Year.Create(Year.MinYear - 1);

        result.Should().BeError(ErrorCode.Validation, Resources.Year_InvalidNumber.Format(Year.MinYear, Year.MaxYear));
    }

    [Fact]
    public void WhenCreateAndYearGreaterThanMax_ThenReturnsError()
    {
        var result = Year.Create(Year.MaxYear + 1);

        result.Should().BeError(ErrorCode.Validation, Resources.Year_InvalidNumber.Format(Year.MinYear, Year.MaxYear));
    }

    [Fact]
    public void WhenCreate_ThenReturnsYear()
    {
        var result = Year.Create(Year.MaxYear - 1).Value;

        result.Number.Should().Be(Year.MaxYear - 1);
    }
}