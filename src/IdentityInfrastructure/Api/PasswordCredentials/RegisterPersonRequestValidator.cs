using Common.Extensions;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class RegisterPersonRequestValidator : AbstractValidator<RegisterPersonPasswordRequest>
{
    public RegisterPersonRequestValidator()
    {
        RuleFor(req => req.FirstName)
            .NotEmpty()
            .Matches(Validations.Credentials.Person.Name)
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidFirstName);
        RuleFor(req => req.LastName)
            .NotEmpty()
            .Matches(Validations.Credentials.Person.Name)
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidLastName);
        RuleFor(dto => dto.EmailAddress)
            .NotEmpty()
            .IsEmailAddress()
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidEmail);
        RuleFor(dto => dto.Password)
            .NotEmpty()
            .Matches(CommonValidations.Passwords.Password.Strict)
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidPassword);
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
        RuleFor(dto => dto.TermsAndConditionsAccepted)
            .NotEmpty()
            .Must(dto => dto)
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidTermsAndConditionsAccepted);
    }
}