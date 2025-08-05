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
public class UpdateOAuth2ClientRequestValidatorSpec
{
    private readonly UpdateOAuth2ClientRequest _dto;
    private readonly UpdateOAuth2ClientRequestValidator _validator;

    public UpdateOAuth2ClientRequestValidatorSpec()
    {
        _validator = new UpdateOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new UpdateOAuth2ClientRequest
        {
            Id = "anid",
            Name = "aclientname",
            RedirectUri = "https://localhost/callback"
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

    [Fact]
    public void WhenNameIsInvalid_ThenThrows()
    {
        _dto.Name = "aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.UpdateOAuth2ClientRequestValidator_InvalidName);
    }

    [Fact]
    public void WhenNameIsNull_ThenSucceeds()
    {
        _dto.Name = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenRedirectUriIsNull_ThenSucceeds()
    {
        _dto.RedirectUri = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenRedirectUriIsInvalid_ThenThrows()
    {
        _dto.RedirectUri = "notaurl";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.UpdateOAuth2ClientRequestValidator_InvalidRedirectUri);
    }
}