using AncillaryInfrastructure.Api.Audits;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryInfrastructure.UnitTests.Api.Audits;

[Trait("Category", "Unit")]
public class DeliverAuditRequestValidatorSpec
{
    private readonly DeliverAuditRequest _dto;
    private readonly DeliverAuditRequestValidator _validator;

    public DeliverAuditRequestValidatorSpec()
    {
        _validator = new DeliverAuditRequestValidator();
        _dto = new DeliverAuditRequest
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