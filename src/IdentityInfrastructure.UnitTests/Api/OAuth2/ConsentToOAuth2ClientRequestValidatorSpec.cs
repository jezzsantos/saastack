using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using IdentityDomain;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class ConsentToOAuth2ClientRequestValidatorSpec
{
    private readonly ConsentToOAuth2ClientForCallerRequest _request;
    private readonly ConsentToOAuth2ClientRequestValidator _validator;

    public ConsentToOAuth2ClientRequestValidatorSpec()
    {
        _validator = new ConsentToOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new ConsentToOAuth2ClientForCallerRequest
        {
            Id = "anid",
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}, {OAuth2Constants.Scopes.Profile}",
            Consented = true
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenScopesIsEmpty_ThenThrows()
    {
        _request.Scope = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidScopes);
    }

    [Fact]
    public void WhenScopesIsNull_ThenSucceeds()
    {
        _request.Scope = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenScopesContainsInvalidScope_ThenThrows()
    {
        _request.Scope = $"{OpenIdConnectConstants.Scopes.OpenId}, aninvalidscope";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidScopes);
    }

    [Fact]
    public void WhenRedirectUriIsInvalid_ThenThrows()
    {
        _request.RedirectUri = "aninvaliduri";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenRedirectUriIsValid_ThenSucceeds()
    {
        _request.RedirectUri = "https://localhost/callback";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenStateIsInvalid_ThenThrows()
    {
        _request.State = "^aninvalidstate^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidState);
    }

    [Fact]
    public void WhenStateIsValid_ThenSucceeds()
    {
        _request.State = "astate";

        _validator.ValidateAndThrow(_request);
    }
}