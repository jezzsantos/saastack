using Domain.Common.Identity;
using FluentValidation;
using ImagesInfrastructure.Api.Images;
using Infrastructure.Web.Api.Operations.Shared.Images;
using Xunit;

namespace ImagesInfrastructure.UnitTests.Api.Images;

[Trait("Category", "Unit")]
public class DownloadImageRequestValidatorSpec
{
    private readonly DownloadImageRequest _dto;
    private readonly DownloadImageRequestValidator _validator;

    public DownloadImageRequestValidatorSpec()
    {
        _validator = new DownloadImageRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new DownloadImageRequest
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