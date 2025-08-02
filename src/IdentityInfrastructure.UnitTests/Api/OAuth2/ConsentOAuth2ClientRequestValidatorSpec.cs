using Domain.Common.Identity;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class ConsentOAuth2ClientRequestValidatorSpec
{
    private readonly ConsentOAuth2ClientForCallerRequest _request;
    private readonly ConsentOAuth2ClientRequestValidator _validator;

    public ConsentOAuth2ClientRequestValidatorSpec()
    {
        _validator = new ConsentOAuth2ClientRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new ConsentOAuth2ClientForCallerRequest
        {
            Id = "anid",
            Scope = $"{OpenIdConnectConstants.Scopes.OpenId}, {OAuth2Constants.Scopes.Profile}",
            Consented = true,
            RedirectUri = "https://localhost/callback",
            State = "astate"
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
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenScopesIsNull_ThenThrows()
    {
        _request.Scope = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenScopesContainsInvalidScope_ThenThrows()
    {
        _request.Scope = $"{OpenIdConnectConstants.Scopes.OpenId}, aninvalidscope";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidScope);
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
    public void WhenStateIsInvalid_ThenThrows()
    {
        _request.State = "^aninvalidstate^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.ConsentToOAuth2ClientRequestValidator_InvalidState);
    }
}