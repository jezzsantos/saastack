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
        RuleFor(dto => dto.Name)
            .NotEmpty()
            .Matches(Validations.Machine.Name)
            .WithMessage(Resources.RegisterMachineRequestValidator_InvalidName);
        RuleFor(dto => dto.Timezone)
            .NotEmpty()
            .Matches(CommonValidations.Timezone)
            .WithMessage(Resources.RegisterAnyRequestValidator_InvalidTimezone)
            .When(dto => dto.Timezone.HasValue());
        RuleFor(dto => dto.CountryCode)
            .NotEmpty()
            .Matches(CommonValidations.CountryCode)
            .WithMessage(Resources.RegisterAnyRequestValidator_InvalidCountryCode)
            .When(dto => dto.CountryCode.HasValue());
    }
}