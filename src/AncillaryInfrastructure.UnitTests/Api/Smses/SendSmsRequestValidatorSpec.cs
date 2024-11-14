using AncillaryInfrastructure.Api.Smses;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.Smses;

[Trait("Category", "Unit")]
public class SendSmsRequestValidatorSpec
{
    private readonly SendSmsRequest _dto;
    private readonly SendSmsRequestValidator _validator;

    public SendSmsRequestValidatorSpec()
    {
        _validator = new SendSmsRequestValidator();
        _dto = new SendSmsRequest
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
            .WithMessageLike(Resources.AnyQueueMessageValidator_InvalidMessage);
    }
}