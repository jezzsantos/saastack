using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.AuthTokens;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(req => req.RefreshToken)
            .NotEmpty()
            .WithMessage(Resources.RefreshTokenRequestValidator_InvalidToken);
    }
}