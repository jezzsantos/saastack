using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.PasswordCredentials;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.PasswordCredentials;

[Trait("Category", "Unit")]
public class ResendPasswordResetRequestValidatorSpec
{
    private readonly ResendPasswordResetRequest _dto;
    private readonly ResendPasswordResetRequestValidator _validator;

    public ResendPasswordResetRequestValidatorSpec()
    {
        _validator = new ResendPasswordResetRequestValidator();
        _dto = new ResendPasswordResetRequest
        {
            Token = new TokensService().CreatePasswordResetToken()
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
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