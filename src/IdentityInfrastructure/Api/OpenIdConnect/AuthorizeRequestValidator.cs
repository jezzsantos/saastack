using Common.Extensions;
using Domain.Common.Identity;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using JetBrains.Annotations;

namespace IdentityInfrastructure.Api.OpenIdConnect;

[UsedImplicitly]
public class AuthorizeRequestValidator : AbstractValidator<OAuth2AuthorizeGetRequest>
{
    public AuthorizeRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.ClientId)
            .IsEntityId(identifierFactory)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidClientId);

        RuleFor(req => req.RedirectUri)
            .NotEmpty()
            .IsUrl()
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidRedirectUri);

        RuleFor(req => req.ResponseType)
            .NotEmpty()
            .Matches(Validations.OAuth2.Code)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidResponseType);

        RuleFor(req => req.Scope)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.Scope)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidScope);

        RuleFor(req => req.State)
            .Matches(Validations.OAuth2.State)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidState)
            .When(req => req.State.HasValue());

        RuleFor(req => req.Nonce)
            .Matches(Validations.OAuth2.Nonce)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidNonce)
            .When(req => req.Nonce.HasValue());

        RuleFor(req => req.CodeChallenge)
            .Matches(Validations.OAuth2.CodeChallenge)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidCodeChallenge)
            .When(req => req.CodeChallenge.HasValue());

        RuleFor(req => req.CodeChallengeMethod)
            .NotEmpty()
            .Matches(Validations.OAuth2.CodeChallengeMethod)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidCodeChallengeMethod)
            .When(req => req.CodeChallenge.HasValue());
    }
}