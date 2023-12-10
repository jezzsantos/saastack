using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsInfrastructure.Api.Cars;

[UsedImplicitly]
public class TakeOfflineCarRequestValidator : AbstractValidator<TakeOfflineCarRequest>
{
    public TakeOfflineCarRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.FromUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage(Resources.TakeOfflineCarRequestValidator_InvalidFromUtc);
        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .WithMessage(Resources.TakeOfflineCarRequestValidator_InvalidToUtc);
    }
}