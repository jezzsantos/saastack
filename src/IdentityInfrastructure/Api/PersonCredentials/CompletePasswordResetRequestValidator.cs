using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PersonCredentials;

public class CompletePasswordResetRequestValidator : AbstractValidator<CompleteCredentialResetRequest>
{
    public CompletePasswordResetRequestValidator()
    {
        RuleFor(req => req.Password)
            .Matches(CommonValidations.Passwords.Password.Strict)
            .WithMessage(Resources.CompletePasswordResetRequestValidator_InvalidPassword);
        RuleFor(req => req.Token)
            .Matches(Validations.Credentials.Password.ResetToken)
            .WithMessage(Resources.CompletePasswordResetRequestValidator_InvalidToken);
    }
}