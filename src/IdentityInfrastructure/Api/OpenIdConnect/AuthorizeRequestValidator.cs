using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using JetBrains.Annotations;

namespace IdentityInfrastructure.Api.OpenIdConnect;

[UsedImplicitly]
public class AuthorizeRequestValidator : AbstractValidator<AuthorizeRequest>
{
    public AuthorizeRequestValidator()
    {
        RuleFor(req => req.ClientId)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.ClientId)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidClientId);

        RuleFor(req => req.RedirectUri)
            .NotEmpty()
            .IsUrl()
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidRedirectUri);

        RuleFor(req => req.ResponseType)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.Code)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidResponseType);

        RuleFor(req => req.Scope)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.Scope)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidScope);

        RuleFor(req => req.State)
            .Matches(Validations.OpenIdConnect.State)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidState)
            .When(req => req.State.HasValue());

        RuleFor(req => req.Nonce)
            .Matches(Validations.OpenIdConnect.Nonce)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidNonce)
            .When(req => req.Nonce.HasValue());

        RuleFor(req => req.CodeChallenge)
            .Matches(Validations.OpenIdConnect.CodeChallenge)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidCodeChallenge)
            .When(req => req.CodeChallenge.HasValue());

        RuleFor(req => req.CodeChallengeMethod)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.CodeChallengeMethod)
            .WithMessage(Resources.AuthorizeRequestValidator_InvalidCodeChallengeMethod)
            .When(req => req.CodeChallenge.HasValue());
    }
}