using Domain.Interfaces.Entities;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.WebApi.Common.Validation;
using Infrastructure.WebApi.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsApi.Apis.Cars;

[UsedImplicitly]
public class DeleteCarRequestValidator : AbstractValidator<DeleteCarRequest>
{
    public DeleteCarRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}