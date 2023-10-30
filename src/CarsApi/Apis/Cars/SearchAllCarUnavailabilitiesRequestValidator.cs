#if TESTINGONLY
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsApi.Apis.Cars;

[UsedImplicitly]
public class SearchAllCarUnavailabilitiesRequestValidator : AbstractValidator<SearchAllCarUnavailabilitiesRequest>
{
    public SearchAllCarUnavailabilitiesRequestValidator(IIdentifierFactory idFactory,
        IHasSearchOptionsValidator hasSearchOptionsValidator)
    {
        Include(hasSearchOptionsValidator);

        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}
#endif