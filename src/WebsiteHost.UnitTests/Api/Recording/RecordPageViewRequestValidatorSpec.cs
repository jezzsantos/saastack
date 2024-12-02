using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using UnitTesting.Common.Validation;
using WebsiteHost.Api.Recording;
using Xunit;

namespace WebsiteHost.UnitTests.Api.Recording;

[Trait("Category", "Unit")]
public class RecordPageViewRequestValidatorSpec
{
    private readonly RecordPageViewRequest _dto;
    private readonly RecordPageViewRequestValidator _validator;

    public RecordPageViewRequestValidatorSpec()
    {
        _validator = new RecordPageViewRequestValidator();
        _dto = new RecordPageViewRequest
        {
            Path = "apath"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenPathIsNull_ThenThrows()
    {
        _dto.Path = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RecordPageViewRequestValidator_InvalidPath);
    }
}