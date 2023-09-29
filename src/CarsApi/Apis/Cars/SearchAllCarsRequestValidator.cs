using FluentValidation;
using Infrastructure.WebApi.Common.Validation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsApi.Apis.Cars;

[UsedImplicitly]
public class SearchAllCarsRequestValidator : AbstractValidator<SearchAllCarsRequest>
{
    public SearchAllCarsRequestValidator(IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);
    }
}