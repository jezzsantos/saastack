using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsInfrastructure.Api.Cars;

[UsedImplicitly]
public class GetCarRequestValidator : AbstractValidator<GetCarRequest>
{
    public GetCarRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}