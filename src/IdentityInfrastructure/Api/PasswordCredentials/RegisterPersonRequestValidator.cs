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
        RuleFor(req => req.EmailAddress)
            .NotEmpty()
            .IsEmailAddress()
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidEmail);
        RuleFor(req => req.Password)
            .NotEmpty()
            .Matches(CommonValidations.Passwords.Password.Strict)
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidPassword);
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
        RuleFor(req => req.TermsAndConditionsAccepted)
            .NotEmpty()
            .Must(req => req)
            .WithMessage(Resources.RegisterPersonRequestValidator_InvalidTermsAndConditionsAccepted);
    }
}