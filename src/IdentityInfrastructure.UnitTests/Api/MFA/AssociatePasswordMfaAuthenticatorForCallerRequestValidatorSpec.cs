using Application.Resources.Shared;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.MFA;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.MFA;

[Trait("Category", "Unit")]
public class AssociatePasswordMfaAuthenticatorForCallerRequestValidatorSpec
{
    private readonly AssociateCredentialMfaAuthenticatorForCallerRequest _dto;
    private readonly AssociatePasswordMfaAuthenticatorForCallerRequestValidator _validator;

    public AssociatePasswordMfaAuthenticatorForCallerRequestValidatorSpec()
    {
        _validator = new AssociatePasswordMfaAuthenticatorForCallerRequestValidator();
        _dto = new AssociateCredentialMfaAuthenticatorForCallerRequest
        {
            MfaToken = new TokensService().CreateMfaAuthenticationToken(),
            AuthenticatorType = CredentialMfaAuthenticatorType.TotpAuthenticator
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

    [Fact]
    public void WhenIsPhoneIsInvalid_ThenThrows()
    {
        _dto.PhoneNumber = "aninvalidphone";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AssociatePasswordMfaAuthenticatorForCallerRequestValidator_InvalidPhoneNumber);
    }

    [Fact]
    public void WhenAuthenticatorTypeIsNone_ThenThrows()
    {
        _dto.AuthenticatorType = CredentialMfaAuthenticatorType.None;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources
                .PasswordMfaAuthenticatorForCallerRequestValidator_InvalidAuthenticatorType);
    }

    [Fact]
    public void WhenAuthenticatorTypeIsRecoveryCodes_ThenThrows()
    {
        _dto.AuthenticatorType = CredentialMfaAuthenticatorType.RecoveryCodes;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources
                .PasswordMfaAuthenticatorForCallerRequestValidator_InvalidAuthenticatorType);
    }
}