using CarsDomain;
using Common.Extensions;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsApi.Apis.Cars;

[UsedImplicitly]
public class TakeOfflineCarRequestValidator : AbstractValidator<TakeOfflineCarRequest>
{
    public TakeOfflineCarRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.Reason)
            .Matches(Validations.Car.Reason)
            .When(req => req.Reason.HasValue())
            .WithMessage(ValidationResources.TakeOfflineCarRequestValidator_InvalidReason);
        RuleFor(req => req.StartAtUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage(ValidationResources.TakeOfflineCarRequestValidator_InvalidStartAtUtc);
        RuleFor(req => req.EndAtUtc)
            .GreaterThan(req => req.StartAtUtc)
            .WithMessage(ValidationResources.TakeOfflineCarRequestValidator_InvalidEndAtUtc);
    }
}