using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.OAuth2;

public class ConsentToOAuth2ClientRequestValidator : AbstractValidator<ConsentToOAuth2ClientForCallerRequest>
{
    public ConsentToOAuth2ClientRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.Id)
            .IsEntityId(identifierFactory)
            .WithMessage(CommonValidationResources.AnyValidator_InvalidId);

        RuleFor(req => req.Scope)
            .NotEmpty()
            .Matches(Validations.OAuth2.Scope)
            .WithMessage(Resources.ConsentToOAuth2ClientRequestValidator_InvalidScopes)
            .When(req => req.Scope != null);

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