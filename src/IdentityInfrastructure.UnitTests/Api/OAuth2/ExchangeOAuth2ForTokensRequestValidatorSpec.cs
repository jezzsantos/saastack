using Domain.Common.Identity;
using Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using IdentityInfrastructure.Api.OAuth2;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using JetBrains.Annotations;
using UnitTesting.Common.Validation;
using Xunit;
using OAuth2GrantType = Application.Resources.Shared.OAuth2GrantType;

namespace IdentityInfrastructure.UnitTests.Api.OAuth2;

[UsedImplicitly]
public class ExchangeOAuth2ForTokensRequestValidatorSpec
{
    [Trait("Category", "Unit")]
    public class GivenAnyGrant
    {
        private readonly ExchangeOAuth2ForTokensRequest _request;
        private readonly ExchangeOAuth2ForTokensRequestValidator _validator;

        public GivenAnyGrant()
        {
            _validator = new ExchangeOAuth2ForTokensRequestValidator(new FixedIdentifierFactory("aclientid"));
            _request = new ExchangeOAuth2ForTokensRequest
            {
                ClientId = "aclientid",
                ClientSecret = new TokensService().GenerateRandomToken(),
                GrantType = OAuth2GrantType.Authorization_Code,
                Code = OAuth2Constants.ResponseTypes.Code,
                RedirectUri = "https://localhost/callback",
                CodeVerifier = null,
                RefreshToken = null,
                Scope = null
            };
        }

        [Fact]
        public void WhenAllPropertiesValid_ThenSucceeds()
        {
            _validator.ValidateAndThrow(_request);
        }

        [Fact]
        public void WhenClientIdIsNull_ThenThrows()
        {
            _request.ClientId = null;

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidClientId);
        }

        [Fact]
        public void WhenClientSecretIsNull_ThenThrows()
        {
            _request.ClientSecret = null;

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidClientSecret);
        }

        [Fact]
        public void WhenGrantTypeIsNull_ThenThrows()
        {
            _request.GrantType = null;

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidGrantType);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAuthorizationCodeGrant
    {
        private readonly ExchangeOAuth2ForTokensRequest _dto;
        private readonly ExchangeOAuth2ForTokensRequestValidator _validator;

        public GivenAuthorizationCodeGrant()
        {
            _validator = new ExchangeOAuth2ForTokensRequestValidator(new FixedIdentifierFactory("aclientid"));
            _dto = new ExchangeOAuth2ForTokensRequest
            {
                ClientId = "aclientid",
                ClientSecret = new TokensService().GenerateRandomToken(),
                GrantType = OAuth2GrantType.Authorization_Code,
                Code = OAuth2Constants.ResponseTypes.Code,
                RedirectUri = "https://localhost/callback",
                CodeVerifier = new string('a', 43),
                RefreshToken = null,
                Scope = null
            };
        }

        [Fact]
        public void WhenAllPropertiesValid_ThenSucceeds()
        {
            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenCodeIsNull_ThenThrows()
        {
            _dto.Code = null;

            _validator
                .Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidCode);
        }

        [Fact]
        public void WhenRedirectUriIsNull_ThenThrows()
        {
            _dto.RedirectUri = null;

            _validator
                .Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidRedirectUri);
        }

        [Fact]
        public void WhenRedirectUriIsInvalid_ThenThrows()
        {
            _dto.RedirectUri = "aninvalidurl";

            _validator
                .Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidRedirectUri);
        }

        [Fact]
        public void WhenCodeVerifierIsNull_ThenSucceeds()
        {
            _dto.CodeVerifier = null;

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenCodeVerifierIsEmpty_ThenSucceeds()
        {
            _dto.CodeVerifier = string.Empty;

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenCodeVerifierIsInvalid_ThenThrows()
        {
            _dto.CodeVerifier = "a";

            _validator
                .Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidCodeVerifier);
        }

        [Fact]
        public void WhenCodeVerifierIsValid_ThenSucceeds()
        {
            _dto.CodeVerifier = new string('a', 43); // Minimum valid length

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenCodeVerifierIsMaxLength_ThenSucceeds()
        {
            _dto.CodeVerifier = new string('a', 128); // Maximum valid length

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenScopeIsNotNull_ThenThrows()
        {
            _dto.Scope = "ascope";

            _validator
                .Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_ScopeMustBeNull);
        }

        [Fact]
        public void WhenRefreshTokenIsNotNull_ThenThrows()
        {
            _dto.RefreshToken = "arefreshtoken";

            _validator
                .Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_RefreshTokenMustBeNull);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRefreshTokenGrant
    {
        private readonly ExchangeOAuth2ForTokensRequest _request;
        private readonly ExchangeOAuth2ForTokensRequestValidator _validator;

        public GivenRefreshTokenGrant()
        {
            _validator = new ExchangeOAuth2ForTokensRequestValidator(new FixedIdentifierFactory("aclientid"));
            _request = new ExchangeOAuth2ForTokensRequest
            {
                ClientId = "aclientid",
                ClientSecret = new TokensService().GenerateRandomToken(),
                GrantType = OAuth2GrantType.Refresh_Token,
                Code = null,
                RedirectUri = null,
                CodeVerifier = null,
                RefreshToken = new TokensService().GenerateRandomToken(),
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
        public void WhenRefreshTokenIsNull_ThenThrows()
        {
            _request.RefreshToken = null;

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidRefreshToken);
        }

        [Fact]
        public void WhenRefreshTokenIsInvalid_ThenThrows()
        {
            _request.RefreshToken = "^aninvalidtoken^";

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidRefreshToken);
        }

        [Fact]
        public void WhenScopeIsInvalid_ThenThrows()
        {
            _request.Scope = "aninvalidscope";

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_InvalidScope);
        }

        [Fact]
        public void WhenScopeIsNull_ThenSucceeds()
        {
            _request.Scope = null;

            _validator.ValidateAndThrow(_request);
        }

        [Fact]
        public void WhenCodeIsNotNull_ThenThrows()
        {
            _request.Code = "acode";

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_CodeMustBeNull);
        }

        [Fact]
        public void WhenRedirectUriIsNotNull_ThenThrows()
        {
            _request.RedirectUri = "aredirecturi";

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_RedirectUriMustBeNull);
        }

        [Fact]
        public void WhenCodeVerifierIsNotNull_ThenThrows()
        {
            _request.CodeVerifier = "acodeverifier";

            _validator
                .Invoking(x => x.ValidateAndThrow(_request))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.ExchangeOAuth2ForTokensRequestValidator_CodeVerifierMustBeNull);
        }
    }
}