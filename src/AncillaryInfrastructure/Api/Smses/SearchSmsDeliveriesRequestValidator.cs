using AncillaryDomain;
using Common.Extensions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Smses;

public class SearchSmsDeliveriesRequestValidator : AbstractValidator<SearchSmsDeliveriesRequest>
{
    public SearchSmsDeliveriesRequestValidator()
    {
        RuleFor(req => req.SinceUtc)
            .LessThan(DateTime.UtcNow.AddHours(1))
            .When(req => req.SinceUtc.HasValue)
            .WithMessage(Resources.SearchEmailDeliveriesRequestValidator_SinceUtc_TooFuture);

        RuleForEach(req => req.Tags)
            .NotEmpty()
            .Matches(Validations.EmailDelivery.Tag)
            .When(req => req.Tags.Exists())
            .WithMessage(Resources.SearchEmailDeliveriesRequestValidator_InvalidTag);
    }
}