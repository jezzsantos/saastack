using Common.Extensions;
using Domain.Common.Identity;
using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using JetBrains.Annotations;
using OAuth2GrantType = Application.Resources.Shared.OAuth2GrantType;

namespace IdentityInfrastructure.Api.OAuth2;

[UsedImplicitly]
public class ExchangeOAuth2ForTokensRequestValidator : AbstractValidator<ExchangeOAuth2ForTokensRequest>
{
    public ExchangeOAuth2ForTokensRequestValidator(IIdentifierFactory identifierFactory)
    {
        RuleFor(req => req.ClientId)
            .IsEntityId(identifierFactory)
            .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidClientId);

        RuleFor(req => req.ClientSecret)
            .NotEmpty()
            .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidClientSecret);

        RuleFor(req => req.GrantType)
            .IsInEnum()
            .NotNull()
            .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidGrantType);

        When(req => req.GrantType is OAuth2GrantType.Authorization_Code, () =>
        {
            RuleFor(req => req.Code)
                .Matches(Validations.OAuth2.AuthorizationCode)
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidCode);

            RuleFor(req => req.RedirectUri)
                .NotEmpty()
                .IsUrl()
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidRedirectUri);

            RuleFor(req => req.CodeVerifier)
                .Matches(Validations.OAuth2.CodeVerifier)
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidCodeVerifier)
                .When(req => req.CodeVerifier.HasValue());

            RuleFor(req => req.Scope)
                .Null()
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_ScopeMustBeNull);

            RuleFor(req => req.RefreshToken)
                .Null()
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_RefreshTokenMustBeNull);
        });

        When(req => req.GrantType is OAuth2GrantType.Refresh_Token, () =>
        {
            RuleFor(req => req.RefreshToken)
                .NotEmpty()
                .Matches(Validations.OAuth2.RefreshToken)
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidRefreshToken);

            RuleFor(req => req.Scope)
                .Matches(Validations.OAuth2.RefreshTokenScope)
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidScope)
                .When(req => req.Scope.HasValue());

            RuleFor(req => req.Code)
                .Null()
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_CodeMustBeNull);

            RuleFor(req => req.RedirectUri)
                .Null()
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_RedirectUriMustBeNull);

            RuleFor(req => req.CodeVerifier)
                .Null()
                .WithMessage(Resources.ExchangeOAuth2ForTokensRequestValidator_CodeVerifierMustBeNull);
        });
    }
}