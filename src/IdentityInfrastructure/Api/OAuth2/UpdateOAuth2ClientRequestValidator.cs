using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class UpdateOAuth2ClientRequestValidator : AbstractValidator<UpdateOAuth2ClientRequest>
{
    public UpdateOAuth2ClientRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.Name)
            .Matches(Validations.OAuth2.ClientName)
            .WithMessage(Resources.UpdateOAuth2ClientRequestValidator_InvalidName)
            .When(req => req.Name.HasValue());

        RuleFor(req => req.RedirectUri)
            .IsUrl()
            .WithMessage(Resources.UpdateOAuth2ClientRequestValidator_InvalidRedirectUri)
            .When(req => req.RedirectUri.HasValue());
    }
}