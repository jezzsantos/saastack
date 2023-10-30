using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class ManufacturerSpec
{
    [Fact]
    public void WhenCreateAndMakeUnknown_ThenReturnsError()
    {
        var result = Manufacturer.Create(Year.Create(Year.MinYear).Value, Name.Create("unknown").Value,
            Name.Create(Manufacturer.AllowedModels[0]).Value);

        result.Should().BeError(ErrorCode.Validation, Resources.Manufacturer_UnknownMake);
    }

    [Fact]
    public void WhenCreateAndModelUnknown_ThenReturnsError()
    {
        var result = Manufacturer.Create(Year.Create(Year.MinYear).Value,
            Name.Create(Manufacturer.AllowedMakes[0]).Value,
            Name.Create("unknown").Value);

        result.Should().BeError(ErrorCode.Validation, Resources.Manufacturer_UnknownModel);
    }

    [Fact]
    public void WhenCreate_ThenReturnsManufacturer()
    {
        var result = Manufacturer.Create(Year.Create(Year.MinYear).Value,
            Name.Create(Manufacturer.AllowedMakes[0]).Value,
            Name.Create(Manufacturer.AllowedModels[0]).Value).Value;

        result.Year.Number.Should().Be(Year.MinYear);
        result.Make.Text.Should().Be(Manufacturer.AllowedMakes[0]);
        result.Model.Text.Should().Be(Manufacturer.AllowedModels[0]);
    }
}