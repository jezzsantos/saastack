using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Cars;

namespace CarsInfrastructure.Api.Cars;

public class ReleaseCarAvailabilityRequestValidator : AbstractValidator<ReleaseCarAvailabilityRequest>
{
    public ReleaseCarAvailabilityRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.FromUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage(Resources.ReleaseCarAvailabilityRequestValidator_InvalidFromUtc);
        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .WithMessage(Resources.ReleaseCarAvailabilityRequestValidator_InvalidToUtc);
    }
}