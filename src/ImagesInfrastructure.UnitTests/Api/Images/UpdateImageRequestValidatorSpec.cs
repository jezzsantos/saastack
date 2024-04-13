using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using ImagesInfrastructure.Api.Images;
using Infrastructure.Web.Api.Operations.Shared.Images;
using UnitTesting.Common.Validation;
using Xunit;

namespace ImagesInfrastructure.UnitTests.Api.Images;

[Trait("Category", "Unit")]
public class UpdateImageRequestValidatorSpec
{
    private readonly UpdateImageRequest _dto;
    private readonly UpdateImageRequestValidator _validator;

    public UpdateImageRequestValidatorSpec()
    {
        _validator = new UpdateImageRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new UpdateImageRequest
        {
            Id = "anid",
            Description = "adescription"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenDescriptionIsEmpty_ThenSucceeds()
    {
        _dto.Description = string.Empty;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenDescriptionIsInvalid_ThenThrows()
    {
        _dto.Description = "aninvalideescription^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.UpdateImageRequestValidator_InvalidDescription);
    }
}