using Domain.Common.Identity;
using FluentValidation;
using ImagesInfrastructure.Api.Images;
using Infrastructure.Web.Api.Operations.Shared.Images;
using Xunit;

namespace ImagesInfrastructure.UnitTests.Api.Images;

[Trait("Category", "Unit")]
public class GetImageRequestValidatorSpec
{
    private readonly GetImageRequest _dto;
    private readonly GetImageRequestValidator _validator;

    public GetImageRequestValidatorSpec()
    {
        _validator = new GetImageRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new GetImageRequest
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