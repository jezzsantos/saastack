using CarsApi.Apis.Cars;
using FluentValidation;
using Infrastructure.WebApi.Common.Validation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using Xunit;

namespace CarsApi.UnitTests.Apis.Cars;

[Trait("Category", "Unit")]
public class SearchAllCarsRequestValidatorSpec
{
    private readonly SearchAllCarsRequest _dto;
    private readonly SearchAllCarsRequestValidator _validator;

    public SearchAllCarsRequestValidatorSpec()
    {
        _validator = new SearchAllCarsRequestValidator(new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _dto = new SearchAllCarsRequest();
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }
}