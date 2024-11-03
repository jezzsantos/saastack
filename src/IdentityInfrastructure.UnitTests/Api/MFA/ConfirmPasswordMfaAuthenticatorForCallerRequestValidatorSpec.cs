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
public class ConfirmPasswordMfaAuthenticatorForCallerRequestValidatorSpec
{
    private readonly ConfirmPasswordMfaAuthenticatorForCallerRequest _dto;
    private readonly ConfirmPasswordMfaAuthenticatorForCallerRequestValidator _validator;

    public ConfirmPasswordMfaAuthenticatorForCallerRequestValidatorSpec()
    {
        _validator =
            new ConfirmPasswordMfaAuthenticatorForCallerRequestValidator();
        _dto = new ConfirmPasswordMfaAuthenticatorForCallerRequest
        {
            MfaToken = new TokensService().CreateMfaAuthenticationToken(),
            AuthenticatorType = PasswordCredentialMfaAuthenticatorType.TotpAuthenticator,
            ConfirmationCode = "123456"
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
    public void WhenOobCodeIsNullAndOobAuthenticator_ThenThrows()
    {
        _dto.AuthenticatorType = PasswordCredentialMfaAuthenticatorType.OobEmail;
        _dto.OobCode = null;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConfirmPasswordMfaAuthenticatorForCallerRequestValidator_InvalidOobCode);
    }

    [Fact]
    public void WhenOobCodeIsInvalidAndOobAuthenticator_ThenThrows()
    {
        _dto.AuthenticatorType = PasswordCredentialMfaAuthenticatorType.OobEmail;
        _dto.OobCode = "aninvalidoobcode";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConfirmPasswordMfaAuthenticatorForCallerRequestValidator_InvalidOobCode);
    }

    [Fact]
    public void WhenConfirmationCodeIsInvalid_ThenThrows()
    {
        _dto.ConfirmationCode = "aninvalidconfirmationcode";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources
                .ConfirmPasswordMfaAuthenticatorForCallerRequestValidator_InvalidConfirmationCode);
    }

    [Fact]
    public void WhenAuthenticatorTypeIsNone_ThenThrows()
    {
        _dto.AuthenticatorType = PasswordCredentialMfaAuthenticatorType.None;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources
                .PasswordMfaAuthenticatorForCallerRequestValidator_InvalidAuthenticatorType);
    }

    [Fact]
    public void WhenAuthenticatorTypeIsRecoveryCodes_ThenThrows()
    {
        _dto.AuthenticatorType = PasswordCredentialMfaAuthenticatorType.RecoveryCodes;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources
                .PasswordMfaAuthenticatorForCallerRequestValidator_InvalidAuthenticatorType);
    }
}