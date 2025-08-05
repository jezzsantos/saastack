using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.SSO;

public class AuthenticateSingleSignOnRequestValidator : AbstractValidator<AuthenticateSingleSignOnRequest>
{
    public AuthenticateSingleSignOnRequestValidator()
    {
        RuleFor(req => req.InvitationToken)
            .Matches(Validations.Credentials.InvitationToken)
            .WithMessage(Resources.AuthenticateSingleSignOnRequestValidator_InvalidInvitationToken)
            .When(req => req.InvitationToken.HasValue());

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

        RuleFor(req => req.CodeVerifier)
            .Matches(Validations.OAuth2.CodeVerifier)
            .WithMessage(Resources.AuthenticateSingleSignOnRequestValidator_InvalidCodeVerifier)
            .When(req => req.CodeVerifier.HasValue());
    }
}