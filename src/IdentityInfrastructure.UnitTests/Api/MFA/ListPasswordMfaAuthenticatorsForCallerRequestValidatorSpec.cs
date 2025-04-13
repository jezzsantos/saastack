using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.MFA;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.MFA;

[Trait("Category", "Unit")]
public class ListPasswordMfaAuthenticatorsForCallerRequestValidatorSpec
{
    private readonly ListCredentialMfaAuthenticatorsForCallerRequest _dto;
    private readonly ListPasswordMfaAuthenticatorsForCallerRequestValidator _validator;

    public ListPasswordMfaAuthenticatorsForCallerRequestValidatorSpec()
    {
        _validator = new ListPasswordMfaAuthenticatorsForCallerRequestValidator();
        _dto = new ListCredentialMfaAuthenticatorsForCallerRequest
        {
            MfaToken = new TokensService().CreateMfaAuthenticationToken()
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
            .WithMessageLike(Resources.PasswordMfaAuthenticatorForCallerRequestValidator_InvalidMfaToken);
    }
}