using Application.Resources.Shared;
using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MFA;

public class
    AssociatePasswordMfaAuthenticatorForCallerRequestValidator : AbstractValidator<
    AssociatePasswordMfaAuthenticatorForCallerRequest>
{
    public AssociatePasswordMfaAuthenticatorForCallerRequestValidator()
    {
        When(req => req.MfaToken.HasValue(), () =>
        {
            RuleFor(req => req.MfaToken)
                .Matches(Validations.Credentials.Password.MfaToken)
                .WithMessage(Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidMfaToken);
        });
        RuleFor(req => req.AuthenticatorType)
            .IsInEnum()
            .NotNull()
            .Must(type => type != PasswordCredentialMfaAuthenticatorType.None
                          && type != PasswordCredentialMfaAuthenticatorType.RecoveryCodes)
            .WithMessage(Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidAuthenticatorType);
        When(req => req.PhoneNumber.HasValue(), () =>
        {
            RuleFor(req => req.PhoneNumber)
                .NotEmpty()
                .Matches(Validations.Credentials.Password.MfaPhoneNumber)
                .WithMessage(Resources.AssociatePasswordMfaAuthenticatorForCallerRequestValidator_InvalidPhoneNumber);
        });
    }
}