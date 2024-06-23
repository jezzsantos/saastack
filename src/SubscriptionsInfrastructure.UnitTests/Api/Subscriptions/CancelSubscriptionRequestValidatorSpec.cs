using Domain.Common.Identity;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api.Subscriptions;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api.Subscriptions;

[Trait("Category", "Unit")]
public class CancelSubscriptionSubscriptionRequestValidatorSpec
{
    private readonly CancelSubscriptionRequest _dto;
    private readonly CancelSubscriptionRequestValidator _validator;

    public CancelSubscriptionSubscriptionRequestValidatorSpec()
    {
        _validator = new CancelSubscriptionRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new CancelSubscriptionRequest
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