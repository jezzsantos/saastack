using Domain.Common.Identity;
using Domain.Interfaces.Validations;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class RegenerateOAuth2ClientSecretRequestValidatorSpec
{
    private readonly RegenerateOAuth2ClientSecretRequest _dto;
    private readonly RegenerateOAuth2ClientSecretRequestValidator _validator;

    public RegenerateOAuth2ClientSecretRequestValidatorSpec()
    {
        _validator = new RegenerateOAuth2ClientSecretRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new RegenerateOAuth2ClientSecretRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenIdIsInvalid_ThenThrows()
    {
        _dto.Id = "aninvalidid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(CommonValidationResources.AnyValidator_InvalidId);
    }
}