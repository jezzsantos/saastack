using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Interfaces.Operations.Cars;
using JetBrains.Annotations;

namespace CarsInfrastructure.Api.Cars;

[UsedImplicitly]
public class ScheduleMaintenanceCarRequestValidator : AbstractValidator<ScheduleMaintenanceCarRequest>
{
    public ScheduleMaintenanceCarRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.FromUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage(Resources.ScheduleMaintenanceCarRequestValidator_InvalidFromUtc);
        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .WithMessage(Resources.ScheduleMaintenanceCarRequestValidator_InvalidToUtc);
    }
}