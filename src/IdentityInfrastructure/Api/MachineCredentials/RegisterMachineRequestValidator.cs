using Common.Extensions;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MachineCredentials;

public class RegisterMachineRequestValidator : AbstractValidator<RegisterMachineRequest>
{
    public RegisterMachineRequestValidator()
    {
        RuleFor(req => req.Name)
            .NotEmpty()
            .Matches(Validations.Machine.Name)
            .WithMessage(Resources.RegisterMachineRequestValidator_InvalidName);
        RuleFor(req => req.Timezone)
            .NotEmpty()
            .Matches(CommonValidations.Timezone)
            .WithMessage(Resources.RegisterAnyRequestValidator_InvalidTimezone)
            .When(req => req.Timezone.HasValue());
        RuleFor(req => req.CountryCode)
            .NotEmpty()
            .Matches(CommonValidations.CountryCode)
            .WithMessage(Resources.RegisterAnyRequestValidator_InvalidCountryCode)
            .When(req => req.CountryCode.HasValue());
    }
}