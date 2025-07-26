using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class RegenerateOAuth2ClientSecretRequestValidator : AbstractValidator<RegenerateOAuth2ClientSecretRequest>
{
    public RegenerateOAuth2ClientSecretRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);
    }
}