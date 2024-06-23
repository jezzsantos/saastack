using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsDomain;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class MigrateSubscriptionRequestValidator : AbstractValidator<MigrateSubscriptionRequest>
{
    public MigrateSubscriptionRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.ProviderName)
            .NotEmpty().Matches(Validations.Subscription.ProviderName)
            .WithMessage(Resources.MigrateSubscriptionRequestValidator_InvalidProviderName);
        RuleFor(req => req.ProviderState)
            .NotEmpty()
            .WithMessage(Resources.MigrateSubscriptionRequestValidator_InvalidProviderState);
    }
}