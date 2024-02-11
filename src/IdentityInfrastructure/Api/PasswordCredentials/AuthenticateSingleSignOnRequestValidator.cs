using Common.Extensions;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
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
        RuleFor(req => req.Username)
            .NotEmpty()
            .IsEmailAddress()
            .When(req => req.Username.HasValue())
            .WithMessage(Resources.AuthenticateSingleSignOnRequestValidator_InvalidUsername);
    }
}