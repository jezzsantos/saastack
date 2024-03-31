using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.SSO;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.SSO;

[Trait("Category", "Unit")]
public class AuthenticateSingleSignOnRequestValidatorSpec
{
    private readonly AuthenticateSingleSignOnRequest _dto;
    private readonly AuthenticateSingleSignOnRequestValidator _validator;

    public AuthenticateSingleSignOnRequestValidatorSpec()
    {
        _validator = new AuthenticateSingleSignOnRequestValidator();
        _dto = new AuthenticateSingleSignOnRequest
        {
            Provider = "aprovider",
            AuthCode = "anauthcode",
            Username = null
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenInvitationTokenIsEmpty_ThenSucceeds()
    {
        _dto.InvitationToken = string.Empty;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenInvitationTokenIsInvalid_ThenThrows()
    {
        _dto.InvitationToken = "aninvalidtoken";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateSingleSignOnRequestValidator_InvalidInvitationToken);
    }

    [Fact]
    public void WhenProviderIsEmpty_ThenThrows()
    {
        _dto.Provider = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateSingleSignOnRequestValidator_InvalidProvider);
    }

    [Fact]
    public void WhenAuthCodeIsEmpty_ThenThrows()
    {
        _dto.AuthCode = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateSingleSignOnRequestValidator_InvalidAuthCode);
    }

    [Fact]
    public void WhenUsernameIsNull_ThenSucceeds()
    {
        _dto.Username = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenUsernameIsValid_ThenSucceeds()
    {
        _dto.Username = "auser@company.com";

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenUsernameIsInvalid_ThenThrows()
    {
        _dto.Username = "notanemail";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateSingleSignOnRequestValidator_InvalidUsername);
    }
}