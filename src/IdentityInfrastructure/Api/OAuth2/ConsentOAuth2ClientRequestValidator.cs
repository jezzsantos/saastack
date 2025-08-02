using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class ConsentOAuth2ClientRequestValidator : AbstractValidator<ConsentOAuth2ClientForCallerRequest>
{
    public ConsentOAuth2ClientRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.Scope)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.Scope)
            .WithMessage(Resources.ConsentToOAuth2ClientRequestValidator_InvalidScope);

        RuleFor(req => req.RedirectUri)
            .IsUrl()
            .WithMessage(Resources.ConsentToOAuth2ClientRequestValidator_InvalidRedirectUri)
            .When(req => !string.IsNullOrEmpty(req.RedirectUri));

        RuleFor(req => req.State)
            .Matches(Validations.OAuth2.State)
            .WithMessage(Resources.ConsentToOAuth2ClientRequestValidator_InvalidState)
            .When(req => !string.IsNullOrEmpty(req.State));
    }
}