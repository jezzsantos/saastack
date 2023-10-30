using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsApi.Apis.Cars;

[UsedImplicitly]
public class SearchAllAvailableCarsRequestValidator : AbstractValidator<SearchAllAvailableCarsRequest>
{
    public SearchAllAvailableCarsRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);

        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .When(req => req.ToUtc.HasValue && req.FromUtc.HasValue)
            .WithMessage(ValidationResources.SearchAllAvailableCarsRequestValidator_InvalidToUtc);
    }
}