using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class VerifyPasswordResetRequestValidator : AbstractValidator<VerifyPasswordResetRequest>
{
    public VerifyPasswordResetRequestValidator()
    {
        RuleFor(req => req.Token)
            .Matches(Validations.Credentials.Password.ResetToken)
            .WithMessage(Resources.CompletePasswordResetRequestValidator_InvalidToken);
    }
}