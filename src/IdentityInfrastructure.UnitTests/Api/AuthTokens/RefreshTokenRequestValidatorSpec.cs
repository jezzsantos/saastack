using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.AuthTokens;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.AuthTokens;

[Trait("Category", "Unit")]
public class RefreshTokenRequestValidatorSpec
{
    private readonly RefreshTokenRequest _dto;
    private readonly RefreshTokenRequestValidator _validator;

    public RefreshTokenRequestValidatorSpec()
    {
        _validator = new RefreshTokenRequestValidator();
        _dto = new RefreshTokenRequest
        {
            RefreshToken = "arefreshtoken"
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenRefreshTokenIsEmpty_ThenThrows()
    {
        _dto.RefreshToken = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.RefreshTokenRequestValidator_InvalidToken);
    }
}