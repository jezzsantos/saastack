using FluentAssertions;
using FluentValidation;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using UnitTesting.Common.Validation;
using WebsiteHost;
using WebsiteHost.Api.AuthN;
using Xunit;

namespace Infrastructure.Web.Website.UnitTests.Api.AuthN;

[Trait("Category", "Unit")]
public class AuthenticateRequestValidatorSpec
{
    private readonly AuthenticateRequest _dto;
    private readonly AuthenticateRequestValidator _validator;

    public AuthenticateRequestValidatorSpec()
    {
        _validator = new AuthenticateRequestValidator();
        _dto = new AuthenticateRequest
        {
            Provider = AuthenticationConstants.Providers.Credentials,
            Username = "auser@company.com",
            Password = "1Password!"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenProviderIsEmpty_TheThrows()
    {
        _dto.Provider = string.Empty;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidProvider);
    }

    [Fact]
    public void WhenProviderIsCredentialsAndUsernameIsEmpty_TheThrows()
    {
        _dto.Provider = AuthenticationConstants.Providers.Credentials;
        _dto.Username = string.Empty;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidUsername);
    }

    [Fact]
    public void WhenProviderIsCredentialsAndUsernameIsNotValid_TheThrows()
    {
        _dto.Provider = AuthenticationConstants.Providers.Credentials;
        _dto.Username = "notanemail";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidUsername);
    }

    [Fact]
    public void WhenProviderIsCredentialsAndPasswordIsEmpty_TheThrows()
    {
        _dto.Provider = AuthenticationConstants.Providers.Credentials;
        _dto.Password = string.Empty;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidPassword);
    }

    [Fact]
    public void WhenProviderIsCredentialsAndPasswordIsNotValid_TheThrows()
    {
        _dto.Provider = AuthenticationConstants.Providers.Credentials;
        _dto.Password = "notapassword";

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidPassword);
    }

    [Fact]
    public void WhenProviderIsSingleSignOnAndAuthCodeIsEmpty_TheThrows()
    {
        _dto.Provider = AuthenticationConstants.Providers.SingleSignOn;
        _dto.AuthCode = string.Empty;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidAuthCode);
    }

    [Fact]
    public void WhenProviderIsSingleSignOnAndAuthCodeIsNotEmpty_TheSucceeds()
    {
        _dto.Provider = AuthenticationConstants.Providers.SingleSignOn;
        _dto.AuthCode = "anauthcode";

        _validator.ValidateAndThrow(_dto);
    }
}