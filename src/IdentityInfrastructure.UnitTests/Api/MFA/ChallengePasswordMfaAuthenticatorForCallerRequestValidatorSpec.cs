using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.MFA;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.MFA;

[Trait("Category", "Unit")]
public class ChallengePasswordMfaAuthenticatorForCallerRequestValidatorSpec
{
    private readonly ChallengeCredentialMfaAuthenticatorForCallerRequest _dto;
    private readonly ChallengePasswordMfaAuthenticatorForCallerRequestValidator _validator;

    public ChallengePasswordMfaAuthenticatorForCallerRequestValidatorSpec()
    {
        _validator = new ChallengePasswordMfaAuthenticatorForCallerRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new ChallengeCredentialMfaAuthenticatorForCallerRequest
        {
            MfaToken = new TokensService().CreateMfaAuthenticationToken(),
            AuthenticatorId = "anid"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenMfaTokenInvalid_ThenThrows()
    {
        _dto.MfaToken = "aninvalidtoken";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(
                Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidMfaToken);
    }
}