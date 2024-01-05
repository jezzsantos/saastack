using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.PasswordCredentials;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.PasswordCredentials;

[Trait("Category", "Unit")]
public class AuthenticatePasswordRequestValidatorSpec
{
    private readonly AuthenticatePasswordRequest _dto;
    private readonly AuthenticatePasswordRequestValidator _validator;

    public AuthenticatePasswordRequestValidatorSpec()
    {
        _validator = new AuthenticatePasswordRequestValidator();
        _dto = new AuthenticatePasswordRequest
        {
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
    public void WhenUsernameIsEmpty_ThenThrows()
    {
        _dto.Username = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticatePasswordRequestValidator_InvalidUsername);
    }

    [Fact]
    public void WhenUsernameIsNotEmail_ThenThrows()
    {
        _dto.Username = "notanemail";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticatePasswordRequestValidator_InvalidUsername);
    }

    [Fact]
    public void WhenPasswordIsEmpty_ThenThrows()
    {
        _dto.Password = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthenticatePasswordRequestValidator_InvalidPassword);
    }
}