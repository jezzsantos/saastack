using Domain.Common.Identity;
using FluentAssertions;
using FluentValidation;
using IdentityDomain;
using IdentityInfrastructure.Api.OpenIdConnect;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OpenIdConnect;

[Trait("Category", "Unit")]
public class AuthorizeRequestValidatorSpec
{
    private readonly OAuth2AuthorizeGetRequest _request;
    private readonly AuthorizeRequestValidator _validator;

    public AuthorizeRequestValidatorSpec()
    {
        _validator = new AuthorizeRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new OAuth2AuthorizeGetRequest
        {
            ClientId = "anid",
            RedirectUri = "https://localhost/callback",
            ResponseType = OAuth2Constants.ResponseTypes.Code,
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenClientIdIsEmpty_ThenThrows()
    {
        _request.ClientId = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientIdIsNull_ThenThrows()
    {
        _request.ClientId = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientIdIsInvalidFormat_ThenThrows()
    {
        _request.ClientId = "aninvalidclientid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenRedirectUriIsEmpty_ThenThrows()
    {
        _request.RedirectUri = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenRedirectUriIsInvalid_ThenThrows()
    {
        _request.RedirectUri = "aninvalidurl";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenResponseTypeIsNotCode_ThenThrows()
    {
        _request.ResponseType = "aninvalidresponsetype";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidResponseType);
    }

    [Fact]
    public void WhenScopeDoesNotContainOpenId_ThenThrows()
    {
        _request.Scope = $"{OAuth2Constants.Scopes.Email} {OAuth2Constants.Scopes.Address}";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenScopeIsEmpty_ThenThrows()
    {
        _request.Scope = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenStateIsTooLong_ThenThrows()
    {
        _request.State = new string('a', 501);

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidState);
    }

    [Fact]
    public void WhenNonceIsTooLong_ThenThrows()
    {
        _request.Nonce = new string('a', 501);

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidNonce);
    }

    [Fact]
    public void WhenCodeChallengeIsInvalid_ThenThrows()
    {
        _request.CodeChallenge = "a";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidCodeChallenge);
    }

    [Fact]
    public void WhenCodeChallengeMethodIsInvalid_ThenThrows()
    {
        _request.CodeChallenge = new string('a', 43);
        _request.CodeChallengeMethod = "aninvalidmethod";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidCodeChallengeMethod);
    }

    [Fact]
    public void WhenCodeChallengeProvidedButMethodMissing_ThenThrows()
    {
        _request.CodeChallenge = new string('a', 43);
        _request.CodeChallengeMethod = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeRequestValidator_InvalidCodeChallengeMethod);
    }

    [Fact]
    public void WhenCodeChallengeAndMethodAreValid_ThenSucceeds()
    {
        _request.CodeChallenge = new string('a', 43);
        _request.CodeChallengeMethod = OAuth2Constants.CodeChallengeMethods.S256;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeChallengeMethodIsPlain_ThenSucceeds()
    {
        _request.CodeChallenge = new string('a', 43);
        _request.CodeChallengeMethod = OAuth2Constants.CodeChallengeMethods.Plain;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenOptionalParametersAreNull_ThenSucceeds()
    {
        _request.State = null;
        _request.Nonce = null;
        _request.CodeChallenge = null;
        _request.CodeChallengeMethod = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenOptionalParametersAreEmpty_ThenSucceeds()
    {
        _request.State = string.Empty;
        _request.Nonce = string.Empty;
        _request.CodeChallenge = string.Empty;
        _request.CodeChallengeMethod = string.Empty;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenScopeContainsMultipleValidScopes_ThenSucceeds()
    {
        _request.Scope =
            $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenScopeIsOnlyOpenId_ThenSucceeds()
    {
        _request.Scope = OpenIdConnectConstants.Scopes.OpenId;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRedirectUriIsHttps_ThenSucceeds()
    {
        _request.RedirectUri = "https://localhost/callback";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRedirectUriIsHttp_ThenSucceeds()
    {
        _request.RedirectUri = "http://localhost/callback";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRedirectUriHasQueryParameters_ThenSucceeds()
    {
        _request.RedirectUri = "https://localhost/callback?param=value";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenStateIsMaxLength_ThenSucceeds()
    {
        _request.State = new string('a', 500);

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenNonceIsMaxLength_ThenSucceeds()
    {
        _request.Nonce = new string('a', 500);

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeChallengeIsMaxLength_ThenSucceeds()
    {
        _request.CodeChallenge = new string('a', 128);
        _request.CodeChallengeMethod = OAuth2Constants.CodeChallengeMethods.S256;

        _validator.ValidateAndThrow(_request);
    }
}