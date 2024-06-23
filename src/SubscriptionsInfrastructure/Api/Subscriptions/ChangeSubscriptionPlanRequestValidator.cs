using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsDomain;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class ChangeSubscriptionPlanRequestValidator : AbstractValidator<ChangeSubscriptionPlanRequest>
{
    public ChangeSubscriptionPlanRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.PlanId)
            .Matches(Validations.Subscription.PlanId)
            .WithMessage(Resources.ChangeSubscriptionPlanRequestValidator_InvalidPlanId);
    }
}