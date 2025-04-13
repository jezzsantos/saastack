using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.PersonCredentials;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.PersonCredentials;

[Trait("Category", "Unit")]
public class CompletePasswordResetRequestValidatorSpec
{
    private readonly CompleteCredentialResetRequest _dto;
    private readonly CompletePasswordResetRequestValidator _validator;

    public CompletePasswordResetRequestValidatorSpec()
    {
        _validator = new CompletePasswordResetRequestValidator();
        _dto = new CompleteCredentialResetRequest
        {
            Password = "1Password!",
            Token = new TokensService().CreatePasswordResetToken()
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenPasswordIsEmpty_ThenThrows()
    {
        _dto.Password = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CompletePasswordResetRequestValidator_InvalidPassword);
    }

    [Fact]
    public void WhenPasswordIsInvalid_ThenThrows()
    {
        _dto.Password = "not";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CompletePasswordResetRequestValidator_InvalidPassword);
    }

    [Fact]
    public void WhenTokenIsEmpty_ThenThrows()
    {
        _dto.Token = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CompletePasswordResetRequestValidator_InvalidToken);
    }

    [Fact]
    public void WhenTokenIsInvalid_ThenThrows()
    {
        _dto.Token = "notavalidtoken";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CompletePasswordResetRequestValidator_InvalidToken);
    }
}