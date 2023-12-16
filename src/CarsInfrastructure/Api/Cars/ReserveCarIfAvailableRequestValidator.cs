using CarsDomain;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Cars;

namespace CarsInfrastructure.Api.Cars;

public class ReserveCarIfAvailableRequestValidator : AbstractValidator<ReserveCarIfAvailableRequest>
{
    public ReserveCarIfAvailableRequestValidator(IIdentifierFactory idFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(idFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
        RuleFor(req => req.FromUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage(Resources.ReserveCarIfAvailableRequestValidator_InvalidFromUtc);
        RuleFor(req => req.ToUtc)
            .GreaterThan(req => req.FromUtc)
            .WithMessage(Resources.ReserveCarIfAvailableRequestValidator_InvalidToUtc);
        RuleFor(req => req.ReferenceId)
            .NotEmpty()
            .Matches(Validations.Unavailability.Reference)
            .WithMessage(Resources.ReserveCarIfAvailableRequestValidator_InvalidReferenceId);
    }
}