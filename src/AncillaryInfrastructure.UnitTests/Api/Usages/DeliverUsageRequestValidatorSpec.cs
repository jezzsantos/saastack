using AncillaryInfrastructure.Api.Usages;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.Usages;

[Trait("Category", "Unit")]
public class DeliverUsageRequestValidatorSpec
{
    private readonly DeliverUsageRequest _dto;
    private readonly DeliverUsageRequestValidator _validator;

    public DeliverUsageRequestValidatorSpec()
    {
        _validator = new DeliverUsageRequestValidator();
        _dto = new DeliverUsageRequest
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