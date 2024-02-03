using FluentValidation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.AuthTokens;

public class RevokeRefreshTokenRequestValidator : AbstractValidator<RevokeRefreshTokenRequest>
{
    public RevokeRefreshTokenRequestValidator()
    {
        RuleFor(req => req.RefreshToken)
            .NotEmpty()
            .WithMessage(Resources.RevokeRefreshTokenRequestValidator_InvalidToken);
    }
}