using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class CreateOAuth2ClientRequestValidatorSpec
{
    private readonly CreateOAuth2ClientRequest _dto;
    private readonly CreateOAuth2ClientRequestValidator _validator;

    public CreateOAuth2ClientRequestValidatorSpec()
    {
        _validator = new CreateOAuth2ClientRequestValidator();
        _dto = new CreateOAuth2ClientRequest
        {
            Name = "aname",
            RedirectUri = "https://localhost/callback"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenNameIsNull_ThenThrows()
    {
        _dto.Name = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.CreateOAuth2ClientRequestValidator_InvalidName);
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
            .WithMessageLike(Resources.CreateOAuth2ClientRequestValidator_InvalidRedirectUri);
    }
}