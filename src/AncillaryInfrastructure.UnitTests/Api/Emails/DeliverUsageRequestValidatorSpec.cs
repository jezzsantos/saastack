using AncillaryInfrastructure.Api.Emails;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.Emails;

[Trait("Category", "Unit")]
public class DeliverEmailRequestValidatorSpec
{
    private readonly DeliverEmailRequest _dto;
    private readonly DeliverEmailRequestValidator _validator;

    public DeliverEmailRequestValidatorSpec()
    {
        _validator = new DeliverEmailRequestValidator();
        _dto = new DeliverEmailRequest
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