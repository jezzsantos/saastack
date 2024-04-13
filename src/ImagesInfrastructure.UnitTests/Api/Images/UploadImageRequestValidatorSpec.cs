using FluentAssertions;
using FluentValidation;
using ImagesInfrastructure.Api.Images;
using Infrastructure.Web.Api.Operations.Shared.Images;
using UnitTesting.Common.Validation;
using Xunit;

namespace ImagesInfrastructure.UnitTests.Api.Images;

[Trait("Category", "Unit")]
public class UploadImageRequestValidatorSpec
{
    private readonly UploadImageRequest _dto;
    private readonly UploadImageRequestValidator _validator;

    public UploadImageRequestValidatorSpec()
    {
        _validator = new UploadImageRequestValidator();
        _dto = new UploadImageRequest
        {
            Description = "adescription"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenDescriptionIsEmpty_ThenSuceeds()
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