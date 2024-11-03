using Domain.Common.Identity;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MFA;

public class
    ChallengePasswordMfaAuthenticatorForCallerRequestValidator : AbstractValidator<
    ChallengePasswordMfaAuthenticatorForCallerRequest>
{
    public ChallengePasswordMfaAuthenticatorForCallerRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.MfaToken)
            .Matches(Validations.Credentials.Password.MfaToken)
            .WithMessage(Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidMfaToken);
        RuleFor(req => req.AuthenticatorId)
            .IsEntityId(identifierFactory)
            .WithMessage(Resources.ChallengePasswordMfaAuthenticatorForCallerRequestValidator_InvalidAuthenticatorId);
    }
}