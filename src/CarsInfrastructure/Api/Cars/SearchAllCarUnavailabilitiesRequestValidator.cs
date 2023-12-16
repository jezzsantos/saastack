#if TESTINGONLY
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Cars;
using JetBrains.Annotations;

namespace CarsInfrastructure.Api.Cars;

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