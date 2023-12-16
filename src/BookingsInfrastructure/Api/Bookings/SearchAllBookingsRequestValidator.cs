using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Bookings;
using JetBrains.Annotations;

namespace BookingsInfrastructure.Api.Bookings;

[UsedImplicitly]
public class SearchAllBookingsRequestValidator : AbstractValidator<SearchAllBookingsRequest>
{
    public SearchAllBookingsRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);

        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .When(req => req.ToUtc.HasValue && req.FromUtc.HasValue)
            .WithMessage(Resources.SearchAllBookingsRequestValidator_InvalidToUtc);
    }
}