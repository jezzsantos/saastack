using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PasswordCredentials;

public class ConfirmPersonRegistrationRequestValidator : AbstractValidator<ConfirmRegistrationPersonPasswordRequest>
{
    public ConfirmPersonRegistrationRequestValidator()
    {
        RuleFor(req => req.Token)
            .NotEmpty()
            .Matches(Validations.Credentials.Password.VerificationToken)
            .WithMessage(Resources.ConfirmPersonRegistrationRequestValidator_InvalidToken);
    }
}