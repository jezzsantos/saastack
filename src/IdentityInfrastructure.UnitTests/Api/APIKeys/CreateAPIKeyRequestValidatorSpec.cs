using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.APIKeys;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.APIKeys;

[Trait("Category", "Unit")]
public class CreateAPIKeyRequestValidatorSpec
{
    private readonly CreateAPIKeyRequest _dto;
    private readonly CreateAPIKeyRequestValidator _validator;

    public CreateAPIKeyRequestValidatorSpec()
    {
        _validator = new CreateAPIKeyRequestValidator();
        _dto = new CreateAPIKeyRequest
        {
            ExpiresOnUtc = null
        };
    }

    [Fact]
    public void WhenAllProperties_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenExpiresIsTooSoon_ThenThrows()
    {
        _dto.ExpiresOnUtc = DateTime.UtcNow;

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CreateAPIKeyRequestValidator_InvalidExpiresOn);
    }

    [Fact]
    public void WhenExpiresIsTooLong_ThenThrows()
    {
        _dto.ExpiresOnUtc = DateTime.UtcNow.AddYears(1);

        _validator.Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CreateAPIKeyRequestValidator_InvalidExpiresOn);
    }

    [Fact]
    public void WhenExpiresIsValid_ThenSucceeds()
    {
        _dto.ExpiresOnUtc = DateTime.UtcNow.AddHours(1);

        _validator.ValidateAndThrow(_dto);
    }
}