using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using UnitTesting.Common.Validation;
using WebsiteHost.Api.Recording;
using Xunit;
using Resources = WebsiteHost.Resources;

namespace Infrastructure.Web.Website.UnitTests.Api.Recording;

[Trait("Category", "Unit")]
public class RecordMeasureRequestValidatorSpec
{
    private readonly RecordMeasureRequest _dto;
    private readonly RecordMeasureRequestValidator _validator;

    public RecordMeasureRequestValidatorSpec()
    {
        _validator = new RecordMeasureRequestValidator();
        _dto = new RecordMeasureRequest
        {
            EventName = "aneventname"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEventNameIsNull_ThenThrows()
    {
        _dto.EventName = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AnyRecordingEventNameValidator_InvalidEventName);
    }

    [Fact]
    public void WhenAdditionalIsNull_ThenSucceeds()
    {
        _dto.Additional = null;

        _validator.ValidateAndThrow(_dto);
    }
}