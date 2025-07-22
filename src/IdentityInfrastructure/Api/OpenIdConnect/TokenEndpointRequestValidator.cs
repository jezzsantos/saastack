using Common.Extensions;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using JetBrains.Annotations;

namespace IdentityInfrastructure.Api.OpenIdConnect;

[UsedImplicitly]
public class TokenEndpointRequestValidator : AbstractValidator<TokenEndpointRequest>
{
    public TokenEndpointRequestValidator()
    {
        RuleFor(req => req.GrantType)
            .NotEmpty()
            .Matches(Validations.OpenIdConnect.GrantType)
            .WithMessage(Resources.TokenEndpointRequestValidator_InvalidGrantType);

        RuleFor(req => req.ClientId)
            .NotEmpty()
            .WithMessage(Resources.TokenEndpointRequestValidator_InvalidClientId);

        RuleFor(req => req.ClientSecret)
            .NotEmpty()
            .WithMessage(Resources.TokenEndpointRequestValidator_InvalidClientSecret);

        // Rules for authorization_code grant type
        When(req => req.GrantType == "authorization_code", () =>
        {
            RuleFor(req => req.Code)
                .NotEmpty()
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidCode);

            RuleFor(req => req.RedirectUri)
                .NotEmpty()
                .IsUrl()
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidRedirectUri);

            RuleFor(req => req.CodeVerifier)
                .Matches(Validations.OpenIdConnect.CodeVerifier)
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidCodeVerifier)
                .When(req => req.CodeVerifier.HasValue());
        });

        // Rules for refresh_token grant type
        When(req => req.GrantType == "refresh_token", () =>
        {
            RuleFor(req => req.RefreshToken)
                .NotEmpty()
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidRefreshToken);

            RuleFor(req => req.Scope)
                .Matches(Validations.OpenIdConnect.RefreshTokenScope)
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidScope)
                .When(req => req.Scope.HasValue());
        });
    }
}