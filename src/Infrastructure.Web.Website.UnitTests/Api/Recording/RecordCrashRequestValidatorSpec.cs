using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using UnitTesting.Common.Validation;
using WebsiteHost.Api.Recording;
using Xunit;
using Resources = WebsiteHost.Resources;

namespace Infrastructure.Web.Website.UnitTests.Api.Recording;

[Trait("Category", "Unit")]
public class RecordCrashRequestValidatorSpec
{
    private readonly RecordCrashRequest _dto;
    private readonly RecordCrashRequestValidator _validator;

    public RecordCrashRequestValidatorSpec()
    {
        _validator = new RecordCrashRequestValidator();
        _dto = new RecordCrashRequest
        {
            Message = "amessage"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenMessageIsNull_ThenThrows()
    {
        _dto.Message = null!;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RecordCrashRequestValidator_InvalidMessage);
    }
}