using Common.Extensions;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class RegisterPersonPasswordRequestValidator : AbstractValidator<RegisterPersonPasswordRequest>
{
    public RegisterPersonPasswordRequestValidator()
    {
        RuleFor(req => req.InvitationToken)
            .Matches(Validations.Credentials.InvitationToken)
            .WithMessage(Resources.RegisterPersonPasswordRequestValidator_InvalidInvitationToken)
            .When(req => req.InvitationToken.HasValue());
        RuleFor(req => req.FirstName)
            .NotEmpty()
            .Matches(Validations.Credentials.Person.Name)
            .WithMessage(Resources.RegisterPersonPasswordRequestValidator_InvalidFirstName);
        RuleFor(req => req.LastName)
            .NotEmpty()
            .Matches(Validations.Credentials.Person.Name)
            .WithMessage(Resources.RegisterPersonPasswordRequestValidator_InvalidLastName);
        RuleFor(req => req.EmailAddress)
            .NotEmpty()
            .IsEmailAddress()
            .WithMessage(Resources.RegisterPersonPasswordRequestValidator_InvalidEmail);
        RuleFor(req => req.Password)
            .NotEmpty()
            .Matches(CommonValidations.Passwords.Password.Strict)
            .WithMessage(Resources.RegisterPersonPasswordRequestValidator_InvalidPassword);
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
            .WithMessage(Resources.RegisterPersonPasswordRequestValidator_InvalidTermsAndConditionsAccepted);
    }
}