using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.PersonCredentials;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.PersonCredentials;

[Trait("Category", "Unit")]
public class InitiatePasswordResetRequestValidatorSpec
{
    private readonly InitiatePasswordResetRequest _dto;
    private readonly InitiatePasswordResetRequestValidator _validator;

    public InitiatePasswordResetRequestValidatorSpec()
    {
        _validator = new InitiatePasswordResetRequestValidator();
        _dto = new InitiatePasswordResetRequest
        {
            EmailAddress = "user@company.com"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenEmailAddressIsEmpty_ThenThrows()
    {
        _dto.EmailAddress = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.InitiatePasswordResetRequestValidator_InvalidEmailAddress);
    }

    [Fact]
    public void WhenEmailAddressIsInvalid_ThenThrows()
    {
        _dto.EmailAddress = "notanemail";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.InitiatePasswordResetRequestValidator_InvalidEmailAddress);
    }
}