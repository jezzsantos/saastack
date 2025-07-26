using FluentAssertions;
using FluentValidation;
using IdentityDomain;
using IdentityInfrastructure.Api.OpenIdConnect;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.Api.OpenIdConnect;

[Trait("Category", "Unit")]
public class TokenEndpointRequestValidatorSpec
{
    private readonly TokenEndpointRequest _request;
    private readonly TokenEndpointRequestValidator _validator;

    public TokenEndpointRequestValidatorSpec()
    {
        _validator = new TokenEndpointRequestValidator();
        _request = new TokenEndpointRequest
        {
            GrantType = OAuth2Constants.GrantTypes.AuthorizationCode,
            ClientId = "12345678901234567890123456789012",
            ClientSecret = new TokensService().GenerateRandomToken(),
            Code = OAuth2Constants.ResponseTypes.Code,
            RedirectUri = "https://localhost/callback"
        };
    }

    [Fact]
    public void WhenAuthorizationCodeGrantIsValid_ThenSucceeds()
    {
        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRefreshTokenGrantIsValid_ThenSucceeds()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = "arefreshtoken";
        _request.Code = null;
        _request.RedirectUri = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenGrantTypeIsInvalid_ThenThrows()
    {
        _request.GrantType = "aninvalidgranttype";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidGrantType);
    }

    [Fact]
    public void WhenClientIdIsEmpty_ThenThrows()
    {
        _request.ClientId = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientSecretIsEmpty_ThenThrows()
    {
        _request.ClientSecret = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidClientSecret);
    }

    [Fact]
    public void WhenAuthorizationCodeGrantAndCodeIsEmpty_ThenThrows()
    {
        _request.Code = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidCode);
    }

    [Fact]
    public void WhenAuthorizationCodeGrantAndRedirectUriIsEmpty_ThenThrows()
    {
        _request.RedirectUri = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenAuthorizationCodeGrantAndRedirectUriIsInvalid_ThenThrows()
    {
        _request.RedirectUri = "aninvalidurl";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenCodeVerifierIsInvalid_ThenThrows()
    {
        _request.CodeVerifier = "a";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidCodeVerifier);
    }

    [Fact]
    public void WhenRefreshTokenGrantAndRefreshTokenIsEmpty_ThenThrows()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = string.Empty;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidRefreshToken);
    }

    [Fact]
    public void WhenRefreshTokenGrantAndScopeIsInvalid_ThenThrows()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = "arefreshtoken";
        _request.Scope = "aninvalidscope";

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidScope);
    }

    [Fact]
    public void WhenRefreshTokenGrantAndScopeIsValid_ThenSucceeds()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = "arefreshtoken";
        _request.Scope =
            $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRefreshTokenGrantAndScopeIsEmpty_ThenSucceeds()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = "arefreshtoken";
        _request.Scope = string.Empty;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRefreshTokenGrantAndScopeIsNull_ThenSucceeds()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = "refresh-token";
        _request.Scope = null;

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenRefreshTokenGrantWithOfflineAccess_ThenSucceeds()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = "arefreshtoken";
        _request.Scope = $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.OfflineAccess}";

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeVerifierIsValid_ThenSucceeds()
    {
        _request.CodeVerifier = new string('a', 43); // Minimum valid length

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeVerifierIsMaxLength_ThenSucceeds()
    {
        _request.CodeVerifier = new string('a', 128); // Maximum valid length

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeVerifierIsNull_ThenSucceeds()
    {
        _request.CodeVerifier = null; // Optional parameter

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenCodeVerifierIsEmpty_ThenSucceeds()
    {
        _request.CodeVerifier = string.Empty; // Optional parameter

        _validator.ValidateAndThrow(_request);
    }

    [Fact]
    public void WhenGrantTypeIsNull_ThenThrows()
    {
        _request.GrantType = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidGrantType);
    }

    [Fact]
    public void WhenClientIdIsNull_ThenThrows()
    {
        _request.ClientId = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidClientId);
    }

    [Fact]
    public void WhenClientSecretIsNull_ThenThrows()
    {
        _request.ClientSecret = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidClientSecret);
    }

    [Fact]
    public void WhenAuthorizationCodeGrantAndCodeIsNull_ThenFails()
    {
        _request.Code = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidCode);
    }

    [Fact]
    public void WhenAuthorizationCodeGrantAndRedirectUriIsNull_ThenFails()
    {
        _request.RedirectUri = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidRedirectUri);
    }

    [Fact]
    public void WhenRefreshTokenGrantAndRefreshTokenIsNull_ThenFails()
    {
        _request.GrantType = OAuth2Constants.GrantTypes.RefreshToken;
        _request.RefreshToken = null;

        _validator
            .Invoking(x => x.ValidateAndThrow(_request))
            .Should().Throw<ValidationException>()
            .WithMessageLike(Resources.TokenEndpointRequestValidator_InvalidRefreshToken);
    }
}