using Application.Resources.Shared;
using Common.Extensions;
using Domain.Common.Identity;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using JetBrains.Annotations;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;

namespace IdentityInfrastructure.Api.OAuth2;

[UsedImplicitly]
public class AuthorizeOAuth2GetRequestValidator : AbstractValidator<AuthorizeOAuth2GetRequest>
{
    public AuthorizeOAuth2GetRequestValidator(IIdentifierFactory identifierFactory)
    {
        //Note: changes to this validator need to be reflected in AuthorizeOAuth2PostRequestValidator
        RuleFor(req => req.ClientId)
            .IsEntityId(identifierFactory)
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);

        RuleFor(req => req.RedirectUri)
            .IsUrl()
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri);

        RuleFor(req => req.ResponseType)
            .NotNull()
            .Must(rt => Enum.TryParse(rt, true, out OAuth2ResponseType _))
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidResponseType);

        RuleFor(req => req.Scope)
            .Matches(Validations.OpenIdConnect.Scope)
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidScope);

        RuleFor(req => req.State)
            .Matches(Validations.OAuth2.State)
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidState)
            .When(req => req.State.HasValue());

        RuleFor(req => req.Nonce)
            .Matches(Validations.OAuth2.Nonce)
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidNonce)
            .When(req => req.Nonce.HasValue());

        RuleFor(req => req.CodeChallenge)
            .Matches(Validations.OAuth2.CodeChallenge)
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidCodeChallenge)
            .When(req => req.CodeChallenge.HasValue());

        RuleFor(req => req.CodeChallengeMethod)
            .Must(rt => Enum.TryParse(rt, true, out OpenIdConnectCodeChallengeMethod _))
            .WithMessage(Resources.AuthorizeOAuth2RequestValidator_InvalidCodeChallengeMethod)
            .When(req => req.CodeChallengeMethod.HasValue());
    }
}