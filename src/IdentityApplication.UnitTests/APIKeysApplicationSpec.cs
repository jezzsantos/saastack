using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared.DomainServices;
using Domain.Shared.Identities;
using FluentAssertions;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class APIKeysApplicationSpec
{
    private readonly Mock<IAPIKeyHasherService> _apiKeyHasherService;
    private readonly APIKeysApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IAPIKeysRepository> _repository;
    private readonly Mock<ITokensService> _tokensService;

    public APIKeysApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateAPIKey())
            .Returns(new APIKeyToken
            {
                Key = "akey",
                Prefix = "aprefix",
                Token = "atoken",
                ApiKey = "anapikey"
            });
        _tokensService.Setup(ts => ts.ParseApiKey(It.IsAny<string>()))
            .Returns(new APIKeyToken
            {
                Key = "akey",
                Prefix = "aprefix",
                Token = "atoken",
                ApiKey = "anapikey"
            });
        _apiKeyHasherService = new Mock<IAPIKeyHasherService>();
        _apiKeyHasherService.Setup(khs => khs.HashAPIKey(It.IsAny<string>()))
            .Returns("akeyhash");
        _apiKeyHasherService.Setup(khs => khs.ValidateAPIKeyHash(It.IsAny<string>()))
            .Returns(true);
        _endUsersService = new Mock<IEndUsersService>();
        _repository = new Mock<IAPIKeysRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<APIKeyRoot>(), It.IsAny<CancellationToken>()))
            .Returns((APIKeyRoot root, CancellationToken _) => Task.FromResult<Result<APIKeyRoot, Error>>(root));

        _application = new APIKeysApplication(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _apiKeyHasherService.Object, _endUsersService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenFindUserForAPIKeyAsyncAndNotAValidApiKey_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.ParseApiKey(It.IsAny<string>()))
            .Returns(Optional<APIKeyToken>.None);
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<APIKeyRoot>, Error>>(Optional<APIKeyRoot>.None));

        var result =
            await _application.FindMembershipsForAPIKeyAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenFindUserForAPIKeyAsyncAndApiKeyNotExist_ThenReturnsNone()
    {
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<APIKeyRoot>, Error>>(Optional<APIKeyRoot>.None));

        var result =
            await _application.FindMembershipsForAPIKeyAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenFindUserForAPIKeyAsyncAndUserNotExist_ThenReturnsNone()
    {
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<APIKeyRoot>, Error>>(apiKey.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(Error.EntityNotFound()));

        var result =
            await _application.FindMembershipsForAPIKeyAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Value.Should().BeNone();
        _endUsersService.Verify(
            eus => eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenFindUserForAPIKeyAsync_ThenReturnsApiKey()
    {
        var apiKey = CreateApiKey();
        _repository.Setup(rep => rep.FindByAPIKeyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<APIKeyRoot>, Error>>(apiKey.ToOptional()));
        var user = new EndUserWithMemberships
        {
            Id = "auserid"
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(user));

        var result =
            await _application.FindMembershipsForAPIKeyAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Value.Value.Id.Should().Be("auserid");
        _endUsersService.Verify(
            eus => eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", CancellationToken.None));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenCreateApiKeyWithNoInformationAsync_ThenCreates()
    {
        _caller.Setup(cc => cc.CallerId).Returns("acallerid");

        var result =
            await _application.CreateAPIKeyAsync(_caller.Object, CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        result.Value.Key.Should().Be("anapikey");
        result.Value.UserId.Should().Be("acallerid");
        result.Value.Description.Should().Be("acallerid");
        result.Value.ExpiresOnUtc.Should()
            .BeNear(DateTime.UtcNow.ToNearestMinute().Add(APIKeysApplication.DefaultAPIKeyExpiry),
                TimeSpan.FromMinutes(1));
        _tokensService.Verify(ts => ts.CreateAPIKey());
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(ak =>
            ak.ApiKey.Value.Token == "atoken"
            && ak.ApiKey.Value.KeyHash == "akeyhash"
            && ak.Description == "acallerid"
            && ak.UserId == "acallerid"
            && ak.ExpiresOn.Value!.Value.IsNear(
                DateTime.UtcNow.ToNearestMinute().Add(APIKeysApplication.DefaultAPIKeyExpiry), TimeSpan.FromMinutes(1))
        ), It.IsAny<CancellationToken>()));
    }
#endif
    [Fact]
    public async Task WhenCreateApiKeyAsync_ThenCreates()
    {
        var expiresOn = DateTime.UtcNow.Add(APIKeysApplication.DefaultAPIKeyExpiry).AddMinutes(1);

        var result =
            await _application.CreateAPIKeyAsync(_caller.Object, "auserid", "adescription", expiresOn,
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        result.Value.Key.Should().Be("anapikey");
        result.Value.UserId.Should().Be("auserid");
        result.Value.Description.Should().Be("adescription");
        result.Value.ExpiresOnUtc.Should().Be(expiresOn);
        _tokensService.Verify(ts => ts.CreateAPIKey());
        _repository.Verify(rep => rep.SaveAsync(It.Is<APIKeyRoot>(ak =>
            ak.ApiKey.Value.Token == "atoken"
            && ak.ApiKey.Value.KeyHash == "akeyhash"
            && ak.Description == "adescription"
            && ak.UserId == "auserid"
            && ak.ExpiresOn.Value == expiresOn
        ), It.IsAny<CancellationToken>()));
    }

    private APIKeyRoot CreateApiKey()
    {
        return APIKeyRoot.Create(_recorder.Object, _idFactory.Object, _apiKeyHasherService.Object,
            "auserid".ToId(), new APIKeyToken
            {
                Key = "akey",
                Prefix = "aprefix",
                Token = "atoken",
                ApiKey = "anapikey"
            }).Value;
    }
}