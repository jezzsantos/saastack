using Domain.Common.Identity;
using FluentValidation;
using ImagesInfrastructure.Api.Images;
using Infrastructure.Web.Api.Operations.Shared.Images;
using Xunit;

namespace ImagesInfrastructure.UnitTests.Api.Images;

[Trait("Category", "Unit")]
public class DeleteImageRequestValidatorSpec
{
    private readonly DeleteImageRequest _dto;
    private readonly DeleteImageRequestValidator _validator;

    public DeleteImageRequestValidatorSpec()
    {
        _validator = new DeleteImageRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new DeleteImageRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}