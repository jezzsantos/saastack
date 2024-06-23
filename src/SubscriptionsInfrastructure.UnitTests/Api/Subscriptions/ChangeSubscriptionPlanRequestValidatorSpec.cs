using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsInfrastructure.Api.Subscriptions;
using UnitTesting.Common.Validation;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api.Subscriptions;

[Trait("Category", "Unit")]
public class ChangeSubscriptionPlanSubscriptionRequestValidatorSpec
{
    private readonly ChangeSubscriptionPlanRequest _dto;
    private readonly ChangeSubscriptionPlanRequestValidator _validator;

    public ChangeSubscriptionPlanSubscriptionRequestValidatorSpec()
    {
        _validator = new ChangeSubscriptionPlanRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ChangeSubscriptionPlanRequest
        {
            Id = "anid",
            PlanId = "aplanid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenPlanIdIsEmpty_ThenThrows()
    {
        _dto.PlanId = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeSubscriptionPlanRequestValidator_InvalidPlanId);
    }

    [Fact]
    public void WhenPlanIdIsInvalid_ThenThrows()
    {
        _dto.PlanId = "^aninvalidplanid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ChangeSubscriptionPlanRequestValidator_InvalidPlanId);
    }
}