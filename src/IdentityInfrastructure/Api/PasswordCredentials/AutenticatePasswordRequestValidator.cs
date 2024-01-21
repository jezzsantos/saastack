using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class AuthenticatePasswordRequestValidator : AbstractValidator<AuthenticatePasswordRequest>
{
    public AuthenticatePasswordRequestValidator()
    {
        RuleFor(req => req.Username)
            .NotEmpty()
            .IsEmailAddress()
            .WithMessage(Resources.AuthenticatePasswordRequestValidator_InvalidUsername);
        RuleFor(req => req.Password)
            .NotEmpty()
            .Matches(CommonValidations.Passwords.Password.Strict)
            .WithMessage(Resources.AuthenticatePasswordRequestValidator_InvalidPassword);
    }
}