using Domain.Common.Identity;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api.Subscriptions;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api.Subscriptions;

[Trait("Category", "Unit")]
public class TransferSubscriptionRequestValidatorValidatorSpec
{
    private readonly TransferSubscriptionRequest _dto;
    private readonly TransferSubscriptionRequestValidator _validator;

    public TransferSubscriptionRequestValidatorValidatorSpec()
    {
        _validator = new TransferSubscriptionRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new TransferSubscriptionRequest
        {
            Id = "anid",
            UserId = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}