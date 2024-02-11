using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using JetBrains.Annotations;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests;

[UsedImplicitly]
public class SSOProvidersServiceSpec
{
    [Trait("Category", "Unit")]
    public class GivenNoAuthProviders
    {
        private readonly SSOProvidersService _service;

        public GivenNoAuthProviders()
        {
            var recorder = new Mock<IRecorder>();
            var idFactory = new Mock<IIdentifierFactory>();
            var repository = new Mock<ISSOUsersRepository>();
            var encryptionService = new Mock<IEncryptionService>();
            encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
                .Returns((string value) => value);
            encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
                .Returns((string value) => value);

            _service = new SSOProvidersService(recorder.Object, idFactory.Object, encryptionService.Object,
                new List<ISSOAuthenticationProvider>(),
                repository.Object);
        }

        [Fact]
        public async Task WhenFindByNameAsyncAndNotRegistered_ThenReturnsNone()
        {
            var result = await _service.FindByNameAsync("aname", CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Should().BeNone();
        }

        [Fact]
        public async Task WhenSaveUserInfoAsyncAndProviderNotRegistered_ThenReturnsError()
        {
            var userInfo = new SSOUserInfo(new List<AuthToken>(), "auser@company.com", "afirstname", null,
                Timezones.Default, CountryCodes.Default);

            var result =
                await _service.SaveUserInfoAsync("aprovidername", "auserid".ToId(), userInfo, CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound,
                Resources.SSOProvidersService_UnknownProvider.Format("aprovidername"));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAuthProviders
    {
        private readonly Mock<ISSOUsersRepository> _repository;
        private readonly SSOProvidersService _service;

        public GivenAuthProviders()
        {
            var recorder = new Mock<IRecorder>();
            var idFactory = new Mock<IIdentifierFactory>();
            idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
                .Returns("anid".ToId());
            _repository = new Mock<ISSOUsersRepository>();
            var encryptionService = new Mock<IEncryptionService>();
            encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
                .Returns((string value) => value);
            encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
                .Returns((string value) => value);

            _service = new SSOProvidersService(recorder.Object, idFactory.Object, encryptionService.Object,
                new List<ISSOAuthenticationProvider>
                {
                    new TestSSOAuthenticationProvider()
                },
                _repository.Object);
        }

        [Fact]
        public async Task WhenFindByNameAsyncAndNotRegistered_ThenReturnsNone()
        {
            var result = await _service.FindByNameAsync("aname", CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Should().BeNone();
        }

        [Fact]
        public async Task WhenFindByNameAsyncRegistered_ThenReturnsProvider()
        {
            var result = await _service.FindByNameAsync(TestSSOAuthenticationProvider.Name, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.Value.Should().BeOfType<TestSSOAuthenticationProvider>();
        }

        [Fact]
        public async Task WhenSaveUserInfoAsyncAndProviderNotRegistered_ThenReturnsError()
        {
            var userInfo = new SSOUserInfo(new List<AuthToken>(), "auser@company.com", "afirstname", null,
                Timezones.Default, CountryCodes.Default);

            var result =
                await _service.SaveUserInfoAsync("aprovidername", "auserid".ToId(), userInfo, CancellationToken.None);

            result.Should().BeError(ErrorCode.EntityNotFound,
                Resources.SSOProvidersService_UnknownProvider.Format("aprovidername"));
        }

        [Fact]
        public async Task WhenSaveUserInfoAsyncAndUserNotExists_ThenCreatesAndSavesDetails()
        {
            var userInfo = new SSOUserInfo(new List<AuthToken>(), "auser@company.com", "afirstname", null,
                Timezones.Default, CountryCodes.Default);
            _repository.Setup(rep =>
                    rep.FindUserInfoByUserIdAsync(It.IsAny<string>(), It.IsAny<Identifier>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Optional<SSOUserRoot>.None);

            var result =
                await _service.SaveUserInfoAsync(TestSSOAuthenticationProvider.Name, "auserid".ToId(), userInfo,
                    CancellationToken.None);

            result.Should().BeSuccess();
            _repository.Verify(rep => rep.SaveAsync(It.Is<SSOUserRoot>(user =>
                user.Id == "anid"
                && user.UserId == "auserid"
                && user.EmailAddress.Value.Address == "auser@company.com"
                && user.Name.Value.FirstName == "afirstname"
                && user.Name.Value.LastName == Optional<Name>.None
                && user.Timezone.Value == Timezones.Default
                && user.Address.Value.CountryCode == CountryCodes.Default
            ), It.IsAny<CancellationToken>()));
        }
    }
}

public class TestSSOAuthenticationProvider : ISSOAuthenticationProvider
{
    public const string Name = "atestprovider";

    public Task<Result<SSOUserInfo, Error>> AuthenticateAsync(ICallerContext context, string authCode,
        string? emailAddress,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public string ProviderName => Name;
}