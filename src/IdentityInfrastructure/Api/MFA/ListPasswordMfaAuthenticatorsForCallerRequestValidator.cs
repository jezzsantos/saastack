using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MFA;

public class
    ListPasswordMfaAuthenticatorsForCallerRequestValidator : AbstractValidator<
    ListCredentialMfaAuthenticatorsForCallerRequest>
{
    public ListPasswordMfaAuthenticatorsForCallerRequestValidator()
    {
        When(req => req.MfaToken.HasValue(), () =>
        {
            RuleFor(req => req.MfaToken)
                .Matches(Validations.Credentials.Password.MfaToken)
                .WithMessage(Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidMfaToken);
        });
    }
}