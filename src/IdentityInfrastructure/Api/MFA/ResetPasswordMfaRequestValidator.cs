using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MFA;

public class ResetPasswordMfaRequestValidator : AbstractValidator<ResetPasswordMfaRequest>
{
    public ResetPasswordMfaRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.UserId)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}