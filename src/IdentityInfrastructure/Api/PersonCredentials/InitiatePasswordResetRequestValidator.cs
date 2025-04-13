using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.PersonCredentials;

public class InitiatePasswordResetRequestValidator : AbstractValidator<InitiatePasswordResetRequest>
{
    public InitiatePasswordResetRequestValidator()
    {
        RuleFor(req => req.EmailAddress)
            .IsEmailAddress()
            .WithMessage(Resources.InitiatePasswordResetRequestValidator_InvalidEmailAddress);
    }
}