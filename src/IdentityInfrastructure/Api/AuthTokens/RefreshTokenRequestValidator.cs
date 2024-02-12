using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.AuthTokens;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(req => req.RefreshToken)
            .NotEmpty()
            .Matches(Validations.AuthTokens.RefreshToken)
            .WithMessage(Resources.RefreshTokenRequestValidator_InvalidToken);
    }
}