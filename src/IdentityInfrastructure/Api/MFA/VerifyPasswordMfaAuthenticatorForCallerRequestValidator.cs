using Application.Resources.Shared;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MFA;

public class
    VerifyPasswordMfaAuthenticatorForCallerRequestValidator : AbstractValidator<
    VerifyCredentialMfaAuthenticatorForCallerRequest>
{
    public VerifyPasswordMfaAuthenticatorForCallerRequestValidator()
    {
        RuleFor(req => req.MfaToken)
            .Matches(Validations.Credentials.Password.MfaToken)
            .WithMessage(Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidMfaToken);
        RuleFor(req => req.AuthenticatorType)
            .IsInEnum()
            .NotNull()
            .Must(type => type != CredentialMfaAuthenticatorType.None)
            .WithMessage(Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidAuthenticatorType);
        When(
            req => req.AuthenticatorType is CredentialMfaAuthenticatorType.OobSms
                or CredentialMfaAuthenticatorType.OobEmail, () =>
            {
                RuleFor(req => req.OobCode)
                    .NotEmpty()
                    .Matches(Validations.Credentials.Password.OobCode)
                    .WithMessage(Resources.ConfirmPasswordMfaAuthenticatorForCallerRequestValidator_InvalidOobCode);
            });
        RuleFor(req => req.ConfirmationCode)
            .NotEmpty()
            .Matches(Validations.Credentials.Password.RecoveryConfirmationCode)
            .When(req => req.AuthenticatorType == CredentialMfaAuthenticatorType.RecoveryCodes)
            .WithMessage(Resources
                .ConfirmPasswordMfaAuthenticatorForCallerRequestValidator_InvalidRecoveryConfirmationCode);
        RuleFor(req => req.ConfirmationCode)
            .NotEmpty()
            .Matches(Validations.Credentials.Password.ConfirmationCode)
            .When(req => req.AuthenticatorType != CredentialMfaAuthenticatorType.RecoveryCodes)
            .WithMessage(Resources.ConfirmPasswordMfaAuthenticatorForCallerRequestValidator_InvalidConfirmationCode);
    }
}