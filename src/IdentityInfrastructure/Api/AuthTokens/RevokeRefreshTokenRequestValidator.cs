using FluentValidation;
using IdentityDomain;
using Infrastructure.Web.Api.Common.Validation;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.AuthTokens;

public class RevokeRefreshTokenRequestValidator : AbstractValidator<RevokeRefreshTokenRequest>
{
    public RevokeRefreshTokenRequestValidator()
    {
        RuleFor(req => req.RefreshToken)
            .NotEmpty()
            .Matches(Validations.AuthTokens.RefreshToken)
            .WithMessage(Resources.RevokeRefreshTokenRequestValidator_InvalidToken);
    }
}