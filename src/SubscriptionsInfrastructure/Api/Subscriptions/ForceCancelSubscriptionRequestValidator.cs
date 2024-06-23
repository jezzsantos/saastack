using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class ForceCancelSubscriptionRequestValidator : AbstractValidator<ForceCancelSubscriptionRequest>
{
    public ForceCancelSubscriptionRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}