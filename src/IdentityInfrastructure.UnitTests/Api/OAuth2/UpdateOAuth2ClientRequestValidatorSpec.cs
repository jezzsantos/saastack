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
    private readonly UpdateOAuth2ClientRequest _request;
    private readonly UpdateOAuth2ClientRequestValidator _validator;

    public UpdateOAuth2ClientRequestValidatorSpec()
    {
        _validator = new UpdateOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new UpdateOAuth2ClientRequest
        {
            Id = "anid",
            Name = "aclientname",
            RedirectUri = "https://localhost/callback"
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

    [Fact]
    public void WhenNameIsInvalid_ThenThrows()
    {
        _request.Name = "aninvalidname^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.UpdateOAuth2ClientRequestValidator_InvalidName);
    }

    [Fact]
    public void WhenNameIsNull_ThenSucceeds()
    {
        _request.Name = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRedirectUriIsNull_ThenSucceeds()
    {
        _request.RedirectUri = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRedirectUriIsInvalid_ThenThrows()
    {
        _request.RedirectUri = "notaurl";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.UpdateOAuth2ClientRequestValidator_InvalidRedirectUri);
    }
}