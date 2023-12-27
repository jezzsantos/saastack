using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using UnitTesting.Common.Validation;
using WebsiteHost;
using WebsiteHost.Api.Recording;
using Xunit;

namespace Infrastructure.Web.Website.UnitTests.Api.Recording;

[Trait("Category", "Unit")]
public class RecordUsageRequestValidatorSpec
{
    private readonly RecordUseRequest _dto;
    private readonly RecordUseRequestValidator _validator;

    public RecordUsageRequestValidatorSpec()
    {
        _validator = new RecordUseRequestValidator();
        _dto = new RecordUseRequest
        {
            EventName = "aneventname",
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