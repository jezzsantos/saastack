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
            .Matches(Validations.OAuth2.GrantType)
            .WithMessage(Resources.TokenEndpointRequestValidator_InvalidGrantType);

        RuleFor(req => req.ClientId)
            .NotEmpty()
            .WithMessage(Resources.TokenEndpointRequestValidator_InvalidClientId);

        RuleFor(req => req.ClientSecret)
            .NotEmpty()
            .WithMessage(Resources.TokenEndpointRequestValidator_InvalidClientSecret);

        When(req => req.GrantType == OAuth2Constants.GrantTypes.AuthorizationCode, () =>
        {
            RuleFor(req => req.Code)
                .NotEmpty()
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidCode);

            RuleFor(req => req.RedirectUri)
                .NotEmpty()
                .IsUrl()
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidRedirectUri);

            RuleFor(req => req.CodeVerifier)
                .Matches(Validations.OAuth2.CodeVerifier)
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidCodeVerifier)
                .When(req => req.CodeVerifier.HasValue());
        });

        When(req => req.GrantType == OAuth2Constants.GrantTypes.RefreshToken, () =>
        {
            RuleFor(req => req.RefreshToken)
                .NotEmpty()
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidRefreshToken);

            RuleFor(req => req.Scope)
                .Matches(Validations.OAuth2.RefreshTokenScope)
                .WithMessage(Resources.TokenEndpointRequestValidator_InvalidScope)
                .When(req => req.Scope.HasValue());
        });
    }
}