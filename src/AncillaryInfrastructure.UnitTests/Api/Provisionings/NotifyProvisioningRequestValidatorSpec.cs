using AncillaryInfrastructure.Api.Provisionings;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.Provisionings;

[Trait("Category", "Unit")]
public class NotifyProvisioningRequestValidatorSpec
{
    private readonly NotifyProvisioningRequest _dto;
    private readonly NotifyProvisioningRequestValidator _validator;

    public NotifyProvisioningRequestValidatorSpec()
    {
        _validator = new NotifyProvisioningRequestValidator();
        _dto = new NotifyProvisioningRequest
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