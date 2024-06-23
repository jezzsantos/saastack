using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsDomain;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class SearchSubscriptionHistoryRequestValidator : AbstractValidator<SearchSubscriptionHistoryRequest>
{
    public SearchSubscriptionHistoryRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator,
        IIdentifierFactory identifierFactory)
    {
        Include(hasSearchOptionsValidator);

        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.FromUtc)
            .GreaterThanOrEqualTo(Validations.Subscription.MinInvoiceDate)
            .When(req => req.FromUtc.HasValue())
            .WithMessage(
                Resources.SearchSubscriptionHistoryRequestValidator_FromUtc_TooPast.Format(Validations.Subscription
                    .MinInvoiceDate));
        RuleFor(req => req.FromUtc)
            .LessThan(Validations.Subscription.MaxInvoiceDate)
            .When(req => req.FromUtc.HasValue())
            .WithMessage(
                Resources.SearchSubscriptionHistoryRequestValidator_FromUtc_TooFuture.Format(Validations.Subscription
                    .MaxInvoiceDate));
        RuleFor(req => req.FromUtc)
            .LessThan(req => req.ToUtc)
            .When(req => req.FromUtc.HasValue() && req.ToUtc.HasValue())
            .WithMessage(Resources.SearchSubscriptionHistoryRequestValidator_FromUtc_StartAfterEnd);
        RuleFor(req => req.ToUtc)
            .GreaterThan(Validations.Subscription.MinInvoiceDate)
            .When(req => req.ToUtc.HasValue())
            .WithMessage(
                Resources.SearchSubscriptionHistoryRequestValidator_ToUtc_TooPast.Format(Validations.Subscription
                    .MinInvoiceDate));
        RuleFor(req => req.ToUtc)
            .LessThan(Validations.Subscription.MaxInvoiceDate)
            .When(req => req.ToUtc.HasValue())
            .WithMessage(
                Resources.SearchSubscriptionHistoryRequestValidator_ToUtc_TooFuture.Format(Validations.Subscription
                    .MaxInvoiceDate));
        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .When(req => req.ToUtc.HasValue() && req.FromUtc.HasValue())
            .WithMessage(Resources.SearchSubscriptionHistoryRequestValidator_ToUtc_EndBeforeStart);
    }
}