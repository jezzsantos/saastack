using Application.Resources.Shared;
using Domain.Common.Identity;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[Trait("Category", "Unit")]
public class AuthorizeOAuth2GetRequestValidatorSpec
{
    private readonly AuthorizeOAuth2GetRequest _dto;
    private readonly AuthorizeOAuth2GetRequestValidator _validator;

    public AuthorizeOAuth2GetRequestValidatorSpec()
    {
        _validator = new AuthorizeOAuth2GetRequestValidator(new FixedIdentifierFactory("anid"));
        _dto = new AuthorizeOAuth2GetRequest
        {
            ClientId = "anid",
            RedirectUri = "https://localhost/callback",
            ResponseType = OAuth2ResponseType.Code.ToString(),
            Scope =
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}",
            Nonce = "anonce",
            CodeChallenge = new string('a', 43),
            CodeChallengeMethod = OpenIdConnectCodeChallengeMethod.S256.ToString(),
            State = "astate"
        };
    }

    [Fact]
    public void WhenAllPropertiesValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenClientIdIsEmpty_ThenThrows()
    {
        _dto.ClientId = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientIdIsNull_ThenThrows()
    {
        _dto.ClientId = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientIdIsInvalid_ThenThrows()
    {
        _dto.ClientId = "aninvalidclientid";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenRedirectUriIsEmpty_ThenThrows()
    {
        _dto.RedirectUri = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenRedirectUriIsInvalid_ThenThrows()
    {
        _dto.RedirectUri = "aninvalidurl";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenRedirectUriHasQueryParameters_ThenSucceeds()
    {
        _dto.RedirectUri = "https://localhost/callback?param=value";

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenScopeIsEmpty_ThenThrows()
    {
        _dto.Scope = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenScopeIsUnknown_ThenThrows()
    {
        _dto.Scope = "ascope";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenNonceIsNull_ThenSucceeds()
    {
        _dto.Nonce = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenNonceIsTooLong_ThenThrows()
    {
        _dto.Nonce = new string('a', 501);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidNonce);
    }

    [Fact]
    public void WhenNonceIsMaxLength_ThenSucceeds()
    {
        _dto.Nonce = new string('a', 500);

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenCodeChallengeIsNull_ThenSucceeds()
    {
        _dto.CodeChallenge = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenCodeChallengeIsInvalid_ThenThrows()
    {
        _dto.CodeChallenge = "^aninvalidcodechallenge^";

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidCodeChallenge);
    }

    [Fact]
    public void WhenCodeChallengeAndMethodAreValid_ThenSucceeds()
    {
        _dto.CodeChallenge = new string('a', 43);
        _dto.CodeChallengeMethod = OpenIdConnectCodeChallengeMethod.S256.ToString();

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenCodeChallengeAndMethodIsNull_ThenSucceeds()
    {
        _dto.CodeChallenge = null;
        _dto.CodeChallengeMethod = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenStateIsNull_ThenSucceeds()
    {
        _dto.State = null;

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenStateIsMaxLength_ThenSucceeds()
    {
        _dto.State = new string('a', 500);

        _validator.ValidateAndThrow(_dto);
    }

    [Fact]
    public void WhenStateIsTooLong_ThenThrows()
    {
        _dto.State = new string('a', 501);

        _validator
            .Invoking(x => x.ValidateAndThrow(_dto))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.AuthorizeOAuth2RequestValidator_InvalidState);
    }
}