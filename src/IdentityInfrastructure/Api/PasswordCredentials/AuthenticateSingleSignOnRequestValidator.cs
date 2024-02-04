using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class AuthenticateSingleSignOnRequestValidator : AbstractValidator<AuthenticateSingleSignOnRequest>
{
    public AuthenticateSingleSignOnRequestValidator()
    {
        RuleFor(req => req.Provider)
            .NotEmpty()
            .WithMessage(Resources.AuthenticateSingleSignOnRequestValidator_InvalidProvider);
        RuleFor(req => req.AuthCode)
            .NotEmpty()
            .WithMessage(Resources.AuthenticateSingleSignOnRequestValidator_InvalidAuthCode);
    }
}