using CarsDomain;
using CarsInfrastructure.Api.Cars;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using UnitTesting.Common.Validation;
using Xunit;

namespace CarsInfrastructure.UnitTests.Api.Cars;

[Trait("Category", "Unit")]
public class RegisterCarRequestValidatorSpec
{
    private readonly RegisterCarRequest _dto;
    private readonly RegisterCarRequestValidator _validator;

    public RegisterCarRequestValidatorSpec()
    {
        _validator = new RegisterCarRequestValidator();
        _dto = new RegisterCarRequest
        {
            Make = "amake",
            Model = "amodel",
            Year = Validations.Car.Year.Min,
            Jurisdiction = Jurisdiction.AllowedCountries[0],
            NumberPlate = "aplate"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenMakeIsNull_ThenThrows()
    {
        _dto.Make = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidMake);
    }

    [Fact]
    public void WhenModelIsNull_ThenThrows()
    {
        _dto.Model = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidModel);
    }

    [Fact]
    public void WhenYearIsLessThanMin_ThenThrows()
    {
        _dto.Year = Validations.Car.Year.Min - 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidYear);
    }

    [Fact]
    public void WhenYearIsMoreThanMax_ThenThrows()
    {
        _dto.Year = Validations.Car.Year.Max + 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidYear);
    }

    [Fact]
    public void WhenJurisdictionIsNull_ThenThrows()
    {
        _dto.Jurisdiction = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidJurisdiction);
    }

    [Fact]
    public void WhenJurisdictionIsInvalid_ThenThrows()
    {
        _dto.Jurisdiction = "notajurisdiction";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidJurisdiction);
    }

    [Fact]
    public void WhenNumberPlateIsNull_ThenThrows()
    {
        _dto.NumberPlate = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidNumberPlate);
    }

    [Fact]
    public void WhenNumberPlateIsInvalid_ThenThrows()
    {
        _dto.NumberPlate = "^aninvalidplate^";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RegisterCarRequestValidator_InvalidNumberPlate);
    }
}