using CarsApi.Apis.Cars;
using Common.Extensions;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using UnitTesting.Common.Validation;
using Xunit;

namespace CarsApi.UnitTests.Apis.Cars;

[Trait("Category", "Unit")]
public class SearchAllAvailableCarsRequestValidatorSpec
{
    private readonly SearchAllAvailableCarsRequest _dto;
    private readonly SearchAllAvailableCarsRequestValidator _validator;

    public SearchAllAvailableCarsRequestValidatorSpec()
    {
        _validator =
            new SearchAllAvailableCarsRequestValidator(new HasSearchOptionsValidator(new HasGetOptionsValidator()));
        _dto = new SearchAllAvailableCarsRequest();
    }

    [Fact]
    public void WhenAllProperties_ThenSuccess()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenFromUtcIsNull_ThenSucceeds()
    {
        _dto.FromUtc = null;
        _dto.ToUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenToUtcIsNull_ThenSucceeds()
    {
        _dto.FromUtc = DateTime.UtcNow.SubtractHours(1);
        _dto.ToUtc = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenToUtcIsLessThanFromUtc_ThenThrows()
    {
        _dto.FromUtc = DateTime.UtcNow;
        _dto.ToUtc = DateTime.UtcNow.SubtractHours(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(ValidationResources.SearchAllAvailableCarsRequestValidator_InvalidToUtc);
    }
}