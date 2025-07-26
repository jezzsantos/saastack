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
    private readonly RegenerateOAuth2ClientSecretRequest _request;
    private readonly RegenerateOAuth2ClientSecretRequestValidator _validator;

    public RegenerateOAuth2ClientSecretRequestValidatorSpec()
    {
        _validator = new RegenerateOAuth2ClientSecretRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new RegenerateOAuth2ClientSecretRequest
        {
            Id = "anid"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenIdIsInvalid_ThenThrows()
    {
        _request.Id = "aninvalidid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(CommonValidationResources.AnyValidator_InvalidId);
    }
}