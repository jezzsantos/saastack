using FluentAssertions;
using FluentValidation;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using JetBrains.Annotations;
using UnitTesting.Common.Validation;
using WebsiteHost;
using WebsiteHost.Api.AuthN;
using Xunit;

namespace Infrastructure.Web.Website.UnitTests.Api.AuthN;

[UsedImplicitly]
public class AuthenticateRequestValidatorSpec
{
    [Trait("Category", "Unit")]
    public class GivenCredentialsProvider
    {
        private readonly AuthenticateRequest _dto;
        private readonly AuthenticateRequestValidator _validator;

        public GivenCredentialsProvider()
        {
            _validator = new AuthenticateRequestValidator();
            _dto = new AuthenticateRequest
            {
                Provider = AuthenticationConstants.Providers.Credentials,
                Username = "auser@company.com",
                Password = "1Password!"
            };
        }

        [Fact]
        public void WhenAllProperties_ThenSucceeds()
        {
            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenProviderIsEmpty_ThenThrows()
        {
            _dto.Provider = string.Empty;

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidProvider);
        }

        [Fact]
        public void WhenUsernameIsEmpty_ThenThrows()
        {
            _dto.Provider = AuthenticationConstants.Providers.Credentials;
            _dto.Username = string.Empty;

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidUsername);
        }

        [Fact]
        public void WhenUsernameIsNotValid_ThenThrows()
        {
            _dto.Provider = AuthenticationConstants.Providers.Credentials;
            _dto.Username = "notanemail";

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidUsername);
        }

        [Fact]
        public void WhenPasswordIsEmpty_ThenThrows()
        {
            _dto.Provider = AuthenticationConstants.Providers.Credentials;
            _dto.Password = string.Empty;

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidPassword);
        }

        [Fact]
        public void WhenPasswordIsNotValid_ThenThrows()
        {
            _dto.Provider = AuthenticationConstants.Providers.Credentials;
            _dto.Password = "notapassword";

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidPassword);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAnotherProvider
    {
        private readonly AuthenticateRequest _dto;
        private readonly AuthenticateRequestValidator _validator;

        public GivenAnotherProvider()
        {
            _validator = new AuthenticateRequestValidator();
            _dto = new AuthenticateRequest
            {
                Provider = "aprovider",
                AuthCode = "anauthcode"
            };
        }

        [Fact]
        public void WhenAllProperties_ThenSucceeds()
        {
            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenProviderIsEmpty_ThenThrows()
        {
            _dto.Provider = string.Empty;

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidProvider);
        }

        [Fact]
        public void WhenAuthCodeIsEmpty_ThenThrows()
        {
            _dto.Provider = "aprovider";
            _dto.AuthCode = string.Empty;

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidAuthCode);
        }

        [Fact]
        public void WhenAuthCodeIsNotEmpty_ThenSucceeds()
        {
            _dto.Provider = "aprovider";
            _dto.AuthCode = "anauthcode";

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenUsernameIsNotValid_ThenThrows()
        {
            _dto.Provider = "aprovider";
            _dto.AuthCode = "anauthcode";
            _dto.Username = "notanemail";

            _validator.Invoking(x => x.ValidateAndThrow(_dto))
                .Should().Throw<ValidationException>()
                .WithMessageLike(Resources.AuthenticateRequestValidator_InvalidUsername);
        }

        [Fact]
        public void WhenUsernameIsNull_ThenSucceeds()
        {
            _dto.Provider = "aprovider";
            _dto.AuthCode = "anauthcode";
            _dto.Username = null;

            _validator.ValidateAndThrow(_dto);
        }

        [Fact]
        public void WhenUsernameIsValid_ThenSucceeds()
        {
            _dto.Provider = "aprovider";
            _dto.AuthCode = "anauthcode";
            _dto.Username = "auser@company.com";

            _validator.ValidateAndThrow(_dto);
        }
    }
}