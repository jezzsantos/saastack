using Domain.Common.Identity;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api.Subscriptions;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api.Subscriptions;

[Trait("Category", "Unit")]
public class ForceCancelSubscriptionSubscriptionRequestValidatorSpec
{
    private readonly ForceCancelSubscriptionRequest _dto;
    private readonly ForceCancelSubscriptionRequestValidator _validator;

    public ForceCancelSubscriptionSubscriptionRequestValidatorSpec()
    {
        _validator = new ForceCancelSubscriptionRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ForceCancelSubscriptionRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }
}