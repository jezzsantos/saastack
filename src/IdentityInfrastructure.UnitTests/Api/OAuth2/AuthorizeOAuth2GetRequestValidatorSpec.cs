using Domain.Common.Identity;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;
using OAuth2CodeChallengeMethod = Application.Resources.Shared.OAuth2CodeChallengeMethod;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class AuthorizeOAuth2GetRequestValidatorSpec
{
    private readonly AuthorizeOAuth2GetRequest _request;
    private readonly AuthorizeOAuth2GetRequestValidator _validator;

    public AuthorizeOAuth2GetRequestValidatorSpec()
    {
        _validator = new AuthorizeOAuth2GetRequestValidator(new FixedIdentifierFactory("anid"));
        _request = new AuthorizeOAuth2GetRequest
        {
            ClientId = "anid",
            RedirectUri = "https://localhost/callback",
            ResponseType = OAuth2ResponseType.Code,
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Nonce = "anonce",
            CodeChallenge = new string('a', 43),
            CodeChallengeMethod = OAuth2CodeChallengeMethod.S256,
            State = "astate"
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
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientIdIsNull_ThenThrows()
    {
        _request.ClientId = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientIdIsInvalid_ThenThrows()
    {
        _request.ClientId = "aninvalidclientid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenRedirectUriIsEmpty_ThenThrows()
    {
        _request.RedirectUri = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenRedirectUriIsInvalid_ThenThrows()
    {
        _request.RedirectUri = "aninvalidurl";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenRedirectUriHasQueryParameters_ThenSucceeds()
    {
        _request.RedirectUri = "https://localhost/callback?param=value";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenScopeIsEmpty_ThenThrows()
    {
        _request.Scope = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenScopeIsUnknown_ThenThrows()
    {
        _request.Scope = "ascope";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenNonceIsNull_ThenSucceeds()
    {
        _request.Nonce = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenNonceIsTooLong_ThenThrows()
    {
        _request.Nonce = new string('a', 501);

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidNonce);
    }

    [Fact]
    public void WhenNonceIsMaxLength_ThenSucceeds()
    {
        _request.Nonce = new string('a', 500);

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeChallengeIsNull_ThenSucceeds()
    {
        _request.CodeChallenge = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeChallengeIsInvalid_ThenThrows()
    {
        _request.CodeChallenge = "acodechallenge";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidCodeChallenge);
    }

    [Fact]
    public void WhenCodeChallengeAndMethodAreValid_ThenSucceeds()
    {
        _request.CodeChallenge = new string('a', 43);
        _request.CodeChallengeMethod = OAuth2CodeChallengeMethod.S256;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeChallengeAndMethodIsNull_ThenSucceeds()
    {
        _request.CodeChallenge = null;
        _request.CodeChallengeMethod = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenStateIsNull_ThenSucceeds()
    {
        _request.State = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenStateIsMaxLength_ThenSucceeds()
    {
        _request.State = new string('a', 500);

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenStateIsTooLong_ThenThrows()
    {
        _request.State = new string('a', 501);

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidState);
    }
}