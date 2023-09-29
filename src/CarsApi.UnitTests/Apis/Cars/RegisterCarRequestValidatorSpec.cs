using CarsApi.Apis.Cars;
using CarsDomain;
using FluentAssertions;
using FluentValidation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using UnitTesting.Common.Validation;
using Xunit;

namespace CarsApi.UnitTests.Apis.Cars;

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
            Year = Validations.Car.Year.Min
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

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.RegisterCarRequestValidator_InvalidMake);
    }

    [Fact]
    public void WhenModelIsNull_ThenThrows()
    {
        _dto.Model = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.RegisterCarRequestValidator_InvalidModel);
    }

    [Fact]
    public void WhenYearIsLessThanMin_ThenThrows()
    {
        _dto.Year = Validations.Car.Year.Min - 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.RegisterCarRequestValidator_InvalidYear);
    }

    [Fact]
    public void WhenYearIsMoreThanMax_ThenThrows()
    {
        _dto.Year = Validations.Car.Year.Max + 1;

        _validator.Invoking(x => x.ValidateAndThrow(_dto)).Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.RegisterCarRequestValidator_InvalidYear);
    }
}