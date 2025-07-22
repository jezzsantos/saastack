using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using JetBrains.Annotations;
using Moq;
using UnitTesting.Common;
using Xunit;
using AuthToken = Application.Resources.Shared.AuthToken;
using PersonName = Domain.Shared.PersonName;

namespace IdentityApplication.UnitTests;

[UsedImplicitly]
public class SSOProvidersServiceSpec
{
    [Trait("Category", "Unit")]
    public class GivenNoAuthProviders
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly SSOProvidersService _service;

        public GivenNoAuthProviders()
        {
            _caller = new Mock<ICallerContext>();
            var recorder = new Mock<IRecorder>();
            var idFactory = new Mock<IIdentifierFactory>();
            var repository = new Mock<ISSOUsersRepository>();
            var encryptionService = new Mock<IEncryptionService>();

            _service = new SSOProvidersService(recorder.Object, idFactory.Object, encryptionService.Object,
                new List<ISSOAuthenticationProvider>(),
                repository.Object);
        }

        [Fact]
        public async Task WhenAuthenticateAndNoProvider_ThenReturnsError()
        {
            var result = await _service.AuthenticateUserAsync(_caller.Object, "aprovidername", "anauthcode",
                "ausername", CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound,
                Resources.SSOProvidersService_UnknownProvider.Format("aprovidername"));
        }

        [Fact]
        public async Task WhenSaveInfoOnBehalfOfUserAsyncAndProviderNotRegistered_ThenReturnsError()
        {
            var userInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname", null,
                Timezones.Default, CountryCodes.Default);

            var result =
                await _service.SaveInfoOnBehalfOfUserAsync(_caller.Object, "aprovidername", "auserid".ToId(), userInfo,
                    CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound,
                Resources.SSOProvidersService_UnknownProvider.Format("aprovidername"));
        }

        [Fact]
        public async Task WhenFindUserByProviderAsync_ThenReturnsError()
        {
            var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "anemailaddress", "afirstname",
                "alastname",
                Timezones.Default, CountryCodes.Default);

            var result = await _service.FindUserByProviderAsync(_caller.Object, "aprovidername", authUserInfo,
                CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound,
                Resources.SSOProvidersService_UnknownProvider.Format("aprovidername"));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAuthProviders
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IIdentifierFactory> _idFactory;
        private readonly Mock<ISSOAuthenticationProvider> _provider;
        private readonly Mock<IRecorder> _recorder;
        private readonly Mock<ISSOUsersRepository> _repository;
        private readonly SSOProvidersService _service;

        public GivenAuthProviders()
        {
            _caller = new Mock<ICallerContext>();
            _recorder = new Mock<IRecorder>();
            _idFactory = new Mock<IIdentifierFactory>();
            _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns("anid".ToId());
            _repository = new Mock<ISSOUsersRepository>();
            var encryptionService = new Mock<IEncryptionService>();
            _provider = new Mock<ISSOAuthenticationProvider>();
            _provider.Setup(p => p.ProviderName)
                .Returns("aprovidername");

            _service = new SSOProvidersService(_recorder.Object, _idFactory.Object, encryptionService.Object,
                new List<ISSOAuthenticationProvider>
                {
                    _provider.Object
                },
                _repository.Object);
        }

        [Fact]
        public async Task WhenAuthenticateUserAndProviderNotAuthenticates_ThenReturnsError()
        {
            _provider.Setup(p => p.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Error.NotAuthenticated());

            var result = await _service.AuthenticateUserAsync(_caller.Object, "aprovidername", "anauthcode",
                "ausername", CancellationToken.None);

            result.Should().BeError(ErrorCode.NotAuthenticated);
        }

        [Fact]
        public async Task WhenAuthenticateUserAndProviderReturnInfoWithoutUid_ThenReturnsError()
        {
            var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "", "auser@company.com", "afirstname",
                "alastname",
                Timezones.Default, CountryCodes.Default);
            _provider.Setup(p => p.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authUserInfo);

            var result = await _service.AuthenticateUserAsync(_caller.Object, "aprovidername", "anauthcode",
                "ausername", CancellationToken.None);

            result.Should().BeError(ErrorCode.Validation, Resources.SSOProvidersService_Authentication_MissingUid);
        }

        [Fact]
        public async Task WhenAuthenticateUserAndProviderReturnInfoWithoutEmail_ThenReturnsError()
        {
            var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "", "afirstname", "alastname",
                Timezones.Default, CountryCodes.Default);
            _provider.Setup(p => p.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authUserInfo);

            var result = await _service.AuthenticateUserAsync(_caller.Object, "aprovidername", "anauthcode",
                "ausername", CancellationToken.None);

            result.Should().BeError(ErrorCode.Validation,
                Resources.SSOProvidersService_Authentication_InvalidEmailAddress);
        }

        [Fact]
        public async Task WhenAuthenticateUserAndProviderReturnInfoWithoutFirstNameThenReturnsError()
        {
            var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "", "alastname",
                Timezones.Default, CountryCodes.Default);
            _provider.Setup(p => p.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authUserInfo);

            var result = await _service.AuthenticateUserAsync(_caller.Object, "aprovidername", "anauthcode",
                "ausername", CancellationToken.None);

            result.Should().BeError(ErrorCode.Validation,
                Resources.SSOProvidersService_Authentication_InvalidNames);
        }

        [Fact]
        public async Task WhenAuthenticateUserAndProviderAuthenticates_ThenReturnsUserInfo()
        {
            var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname",
                "alastname",
                Timezones.Default, CountryCodes.Default);
            _provider.Setup(p => p.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authUserInfo);

            var result = await _service.AuthenticateUserAsync(_caller.Object, "aprovidername", "anauthcode",
                "ausername", CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Should().Be(authUserInfo);
        }

        [Fact]
        public async Task WhenFindUserByProviderAsyncAndNoUser_ThenReturnsNone()
        {
            var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "anemailaddress", "afirstname",
                "alastname",
                Timezones.Default, CountryCodes.Default);
            _repository.Setup(repo => repo.FindByProviderUIdAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<SSOUserRoot>.None);

            var result = await _service.FindUserByProviderAsync(_caller.Object, "aprovidername", authUserInfo,
                CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Should().BeNone();
        }

        [Fact]
        public async Task WhenFindUserByProviderAsync_ThenReturnsUser()
        {
            var authUserInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "anemailaddress", "afirstname",
                "alastname",
                Timezones.Default, CountryCodes.Default);
            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object, "aprovidername", "auserid".ToId())
                .Value;
            ssoUser.ChangeDetails("aprovideruid",
                EmailAddress.Create("auser@company.com").Value,
                PersonName.Create("afirstname", "alastname").Value, Timezone.Default,
                Address.Create(CountryCodes.Default).Value);
            _repository.Setup(repo => repo.FindByProviderUIdAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());

            var result = await _service.FindUserByProviderAsync(_caller.Object, "aprovidername", authUserInfo,
                CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Value.Id.Should().Be("auserid");
            result.Value.Value.ProviderUId.Should().Be("aprovideruid");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAnAuthProvider
    {
        private readonly Mock<ICallerContext> _caller;
        private readonly Mock<IEncryptionService> _encryptionService;
        private readonly Mock<IIdentifierFactory> _idFactory;
        private readonly Mock<IRecorder> _recorder;
        private readonly Mock<ISSOUsersRepository> _repository;
        private readonly SSOProvidersService _service;

        public GivenAnAuthProvider()
        {
            _caller = new Mock<ICallerContext>();
            _recorder = new Mock<IRecorder>();
            _idFactory = new Mock<IIdentifierFactory>();
            _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns("anid".ToId());
            _repository = new Mock<ISSOUsersRepository>();
            _repository.Setup(rep => rep.SaveAsync(It.IsAny<SSOUserRoot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SSOUserRoot root, CancellationToken _) => root);
            _encryptionService = new Mock<IEncryptionService>();
            _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
                .Returns((string _) => "anencryptedvalue");
            _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
                .Returns((string _) => "adecryptedvalue");

            _service = new SSOProvidersService(_recorder.Object, _idFactory.Object, _encryptionService.Object,
                new List<ISSOAuthenticationProvider>
                {
                    new TestSSOAuthenticationProvider()
                },
                _repository.Object);
        }

        [Fact]
        public async Task WhenSaveInfoOnBehalfOfUserAsyncAndProviderNotRegistered_ThenReturnsError()
        {
            var userInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname", null,
                Timezones.Default, CountryCodes.Default);

            var result =
                await _service.SaveInfoOnBehalfOfUserAsync(_caller.Object, "aprovidername", "auserid".ToId(), userInfo,
                    CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound,
                Resources.SSOProvidersService_UnknownProvider.Format("aprovidername"));
        }

        [Fact]
        public async Task WhenSaveInfoOnBehalfOfUserAsyncAndUserNotExists_ThenCreatesAndSavesDetails()
        {
            var userInfo = new SSOAuthUserInfo(new List<AuthToken>(), "auid", "auser@company.com", "afirstname", null,
                Timezones.Default, CountryCodes.Default);
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<SSOUserRoot>.None);
            _repository.Setup(rep => rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<ProviderAuthTokensRoot>.None);

            var result =
                await _service.SaveInfoOnBehalfOfUserAsync(_caller.Object, TestSSOAuthenticationProvider.Name,
                    "auserid".ToId(),
                    userInfo,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep => rep.SaveAsync(It.Is<SSOUserRoot>(user =>
                user.Id == "anid"
                && user.ProviderName == TestSSOAuthenticationProvider.Name
                && user.UserId == "auserid"
                && user.EmailAddress.Value.Address == "auser@company.com"
                && user.Name.Value.FirstName == "afirstname"
                && user.Name.Value.LastName == Optional<Name>.None
                && user.Timezone.Value == Timezones.Default
                && user.Address.Value.CountryCode == CountryCodes.Default
            ), It.IsAny<CancellationToken>()));
            _repository.Verify(rep => rep.FindProviderTokensByUserIdAndProviderAsync(TestSSOAuthenticationProvider.Name,
                "auserid".ToId(), It.IsAny<CancellationToken>()));
            _repository.Verify(rep => rep.SaveAsync(It.Is<ProviderAuthTokensRoot>(toks =>
                toks.ProviderName == TestSSOAuthenticationProvider.Name
                && toks.UserId == "auserid"
                && toks.Tokens.Value.ToList().Count == 0
            ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenSaveTokensOnBehalfOfUserAsyncAndProviderNotRegistered_ThenReturnsError()
        {
            var tokens = new ProviderAuthenticationTokens
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                OtherTokens =
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = default,
                        Type = TokenType.OtherToken,
                        Value = "anothertoken"
                    }
                ]
            };

            var result =
                await _service.SaveTokensOnBehalfOfUserAsync(_caller.Object, "aprovidername", "auserid".ToId(), tokens,
                    CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound,
                Resources.SSOProvidersService_UnknownProvider.Format("aprovidername"));
            _repository.Verify(
                rep => rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _repository.Verify(
                rep => rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _repository.Verify(rep => rep.SaveAsync(It.IsAny<ProviderAuthTokensRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenSaveTokensOnBehalfOfUserAsyncAndUserNotExists_ThenReturnsError()
        {
            var tokens = new ProviderAuthenticationTokens
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                OtherTokens =
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = default,
                        Type = TokenType.OtherToken,
                        Value = "anothertoken"
                    }
                ]
            };
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<SSOUserRoot>.None);

            var result =
                await _service.SaveTokensOnBehalfOfUserAsync(_caller.Object, TestSSOAuthenticationProvider.Name,
                    "auserid".ToId(),
                    tokens, CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound);
            _repository.Verify(rep => rep.FindByUserIdAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(),
                It.IsAny<CancellationToken>()));
            _repository.Verify(
                rep => rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            _repository.Verify(rep => rep.SaveAsync(It.IsAny<ProviderAuthTokensRoot>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenSaveTokensOnBehalfOfUserAsyncWithNoTokens_ThenSavesNewTokens()
        {
            _caller.Setup(cc => cc.CallerId)
                .Returns("auserid");
            var datum = DateTime.UtcNow;
            var tokens = new ProviderAuthenticationTokens
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = datum,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = datum,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                OtherTokens =
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = datum,
                        Type = TokenType.OtherToken,
                        Value = "anothertoken"
                    }
                ]
            };
            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object,
                "aprovidername", "auserid".ToId()).Value;
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());
            _repository.Setup(rep =>
                    rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<ProviderAuthTokensRoot>.None);

            var result =
                await _service.SaveTokensOnBehalfOfUserAsync(_caller.Object, TestSSOAuthenticationProvider.Name,
                    "auserid".ToId(),
                    tokens, CancellationToken.None);

            var expectedTokens = AuthTokens.Create([
                IdentityDomain.AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", datum).Value
            ]).Value;
            result.Should().BeSuccess();
            _repository.Verify(rep => rep.FindByUserIdAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(),
                It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.FindProviderTokensByUserIdAndProviderAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(),
                    It.IsAny<CancellationToken>()));
            _repository.Verify(rep => rep.SaveAsync(It.Is<ProviderAuthTokensRoot>(toks =>
                toks.ProviderName == TestSSOAuthenticationProvider.Name
                && toks.UserId == "auserid"
                && toks.Tokens == expectedTokens
            ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenSaveTokensOnBehalfOfUserAsyncWithExistingTokens_ThenUpdatesTokens()
        {
            _caller.Setup(cc => cc.CallerId)
                .Returns("auserid");
            var datum = DateTime.UtcNow;
            var tokens = new ProviderAuthenticationTokens
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = datum,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = datum,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                OtherTokens =
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = datum,
                        Type = TokenType.OtherToken,
                        Value = "anothertoken"
                    }
                ]
            };
            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object,
                "aprovidername", "auserid".ToId()).Value;
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());
            var providerTokens = ProviderAuthTokensRoot.Create(_recorder.Object, _idFactory.Object,
                TestSSOAuthenticationProvider.Name, "auserid".ToId()).Value;
            providerTokens.ChangeTokens("auserid".ToId(), AuthTokens.Create([
                IdentityDomain.AuthToken.Create(AuthTokenType.AccessToken, "anothervalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.RefreshToken, "anothervalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.OtherToken, "anothervalue", datum).Value
            ]).Value);
            _repository.Setup(rep =>
                    rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(providerTokens.ToOptional());

            var result =
                await _service.SaveTokensOnBehalfOfUserAsync(_caller.Object, TestSSOAuthenticationProvider.Name,
                    "auserid".ToId(), tokens, CancellationToken.None);

            var expectedTokens = AuthTokens.Create([
                IdentityDomain.AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", datum).Value
            ]).Value;
            result.Should().BeSuccess();
            _repository.Verify(rep => rep.FindByUserIdAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(),
                It.IsAny<CancellationToken>()));
            _repository.Verify(rep =>
                rep.FindProviderTokensByUserIdAndProviderAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(),
                    It.IsAny<CancellationToken>()));
            _repository.Verify(rep => rep.SaveAsync(It.Is<ProviderAuthTokensRoot>(toks =>
                toks.ProviderName == TestSSOAuthenticationProvider.Name
                && toks.Id == "anid"
                && toks.UserId == "auserid"
                && toks.Tokens == expectedTokens
            ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenSaveTokensOnBehalfOfUserAsyncWhenCallerIsNotOwner_ThenSavesTokens()
        {
            _caller.Setup(cc => cc.CallerId)
                .Returns("auserid");
            var datum = DateTime.UtcNow;
            var tokens = new ProviderAuthenticationTokens
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = datum,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = datum,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                OtherTokens =
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = datum,
                        Type = TokenType.OtherToken,
                        Value = "anothertoken"
                    }
                ]
            };
            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object,
                "aprovidername", "anotheruserid".ToId()).Value;
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());
            _repository.Setup(rep =>
                    rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<ProviderAuthTokensRoot>.None);

            var result =
                await _service.SaveTokensOnBehalfOfUserAsync(_caller.Object, TestSSOAuthenticationProvider.Name,
                    "anotheruserid".ToId(), tokens, CancellationToken.None);

            var expectedTokens = AuthTokens.Create([
                IdentityDomain.AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", datum).Value
            ]).Value;
            result.Should().BeSuccess();
            _repository.Verify(rep =>
                rep.FindProviderTokensByUserIdAndProviderAsync(TestSSOAuthenticationProvider.Name,
                    "anotheruserid".ToId(),
                    It.IsAny<CancellationToken>()));
            _repository.Verify(rep => rep.SaveAsync(It.Is<ProviderAuthTokensRoot>(toks =>
                toks.ProviderName == TestSSOAuthenticationProvider.Name
                && toks.UserId == "anotheruserid"
                && toks.Tokens == expectedTokens
            ), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenFindProviderByUserId_ThenSucceeds()
        {
            _caller.Setup(cc => cc.CallerId)
                .Returns("auserid");

            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object,
                "aprovidername", "auserid".ToId()).Value;
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());

            var result = await _service.FindProviderByUserIdAsync(_caller.Object, "auserid".ToId(), "atestprovider",
                CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Value.ProviderName.Should().Be("atestprovider");
        }

        [Fact]
        public async Task WhenFindProviderByUserIdAndCallerNotOwner_ThenSucceeds()
        {
            _caller.Setup(cc => cc.CallerId)
                .Returns("auserid");

            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object,
                "aprovidername", "anotheruserid".ToId()).Value;
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());

            var result = await _service.FindProviderByUserIdAsync(_caller.Object, "anotheruserid".ToId(),
                "atestprovider",
                CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Value.ProviderName.Should().Be("atestprovider");
        }

        [Fact]
        public async Task WhenGetTokensForUserAsyncAndNoTokens_ThenReturnsNone()
        {
            _caller.Setup(cc => cc.CallerId)
                .Returns("auserid");
            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object,
                "aprovidername", "auserid".ToId()).Value;
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());
            _repository.Setup(rep => rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(),
                    It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<ProviderAuthTokensRoot>.None);

            var result =
                await _service.GetTokensForUserAsync(_caller.Object, "auserid".ToId(), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(0);
            _repository.Verify(rep => rep.FindByUserIdAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(),
                It.IsAny<CancellationToken>()));
            _repository.Verify(rep => rep.FindProviderTokensByUserIdAndProviderAsync(TestSSOAuthenticationProvider.Name,
                "auserid".ToId(), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task WhenGetTokensForUserAsync_ThenReturnsError()
        {
            _caller.Setup(cc => cc.CallerId)
                .Returns("auserid");
            var datum = DateTime.UtcNow;
            var ssoUser = SSOUserRoot.Create(_recorder.Object, _idFactory.Object,
                "aprovidername", "auserid".ToId()).Value;
            _repository.Setup(rep =>
                    rep.FindByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(ssoUser.ToOptional());
            var expectedTokens = AuthTokens.Create([
                IdentityDomain.AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", datum).Value,
                IdentityDomain.AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", datum).Value
            ]).Value;
            var providerTokens = ProviderAuthTokensRoot.Create(_recorder.Object, _idFactory.Object,
                TestSSOAuthenticationProvider.Name, "auserid".ToId()).Value;
            providerTokens.ChangeTokens("auserid".ToId(), expectedTokens);
            _repository.Setup(rep =>
                    rep.FindProviderTokensByUserIdAndProviderAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(providerTokens.ToOptional());

            var result =
                await _service.GetTokensForUserAsync(_caller.Object, "auserid".ToId(), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Count.Should().Be(1);
            result.Value[0].Provider.Should().Be(TestSSOAuthenticationProvider.Name);
            result.Value[0].AccessToken.ExpiresOn.Should().Be(datum);
            result.Value[0].AccessToken.Type.Should().Be(TokenType.AccessToken);
            result.Value[0].AccessToken.Value.Should().Be("adecryptedvalue");
            result.Value[0].RefreshToken!.ExpiresOn.Should().Be(datum);
            result.Value[0].RefreshToken!.Type.Should().Be(TokenType.RefreshToken);
            result.Value[0].RefreshToken!.Value.Should().Be("adecryptedvalue");
            result.Value[0].OtherTokens.Count.Should().Be(1);
            result.Value[0].OtherTokens[0].Type.Should().Be(TokenType.OtherToken);
            result.Value[0].OtherTokens[0].ExpiresOn.Should().Be(datum);
            result.Value[0].OtherTokens[0].Value.Should().Be("adecryptedvalue");
            _encryptionService.Verify(es => es.Decrypt("anencryptedvalue"), Times.Exactly(3));
            _repository.Verify(rep => rep.FindByUserIdAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(),
                It.IsAny<CancellationToken>()));
        }
    }
}

public class TestSSOAuthenticationProvider : ISSOAuthenticationProvider
{
    public const string Name = "atestprovider";

    public Task<Result<SSOAuthUserInfo, Error>> AuthenticateAsync(ICallerContext caller, string authCode,
        string? emailAddress,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public string ProviderName => Name;

    public Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller,
        string refreshToken, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}