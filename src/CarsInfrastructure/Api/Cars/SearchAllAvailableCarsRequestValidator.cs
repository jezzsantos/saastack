using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using JetBrains.Annotations;

namespace CarsInfrastructure.Api.Cars;

[UsedImplicitly]
public class SearchAllAvailableCarsRequestValidator : AbstractValidator<SearchAllAvailableCarsRequest>
{
    public SearchAllAvailableCarsRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);

        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .When(req => req.ToUtc.HasValue && req.FromUtc.HasValue)
            .WithMessage(Resources.SearchAllAvailableCarsRequestValidator_InvalidToUtc);
    }
}