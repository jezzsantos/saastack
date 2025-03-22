using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.APIKeys;
using Domain.Interfaces.Entities;
using Domain.Shared.Identities;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class APIKeyRootSpec
{
    private readonly APIKeyRoot _apiKey;
    private readonly Mock<IAPIKeyHasherService> _apiKeyHasherService;

    public APIKeyRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _apiKeyHasherService = new Mock<IAPIKeyHasherService>();
        _apiKeyHasherService.Setup(khs => khs.HashAPIKey(It.IsAny<string>()))
            .Returns("akeyhash");
        _apiKeyHasherService.Setup(khs => khs.ValidateKey(It.IsAny<string>()))
            .Returns(true);
        _apiKeyHasherService.Setup(khs => khs.ValidateAPIKeyHash(It.IsAny<string>()))
            .Returns(true);
        _apiKeyHasherService.Setup(khs => khs.VerifyAPIKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _apiKey = APIKeyRoot.Create(recorder.Object, idFactory.Object, _apiKeyHasherService.Object, "auserid".ToId(),
            new APIKeyToken
            {
                Key = "akey",
                Prefix = "aprefix",
                Token = "atoken",
                ApiKey = "anapikey"
            }).Value;
    }

    [Fact]
    public void WhenConstructed_ThenAssigned()
    {
        _apiKey.ApiKey.Value.Token.Should().Be("atoken");
        _apiKey.ApiKey.Value.KeyHash.Should().Be("akeyhash");
        _apiKey.Description.Should().BeNone();
        _apiKey.ExpiresOn.Should().BeNone();
        _apiKey.UserId.Should().Be("auserid".ToId());
    }

    [Fact]
    public void WhenSetParametersAndDescriptionIsInvalid_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.Add(Validations.ApiKey.MinimumExpiryPeriod);
        var result = _apiKey.SetParameters("^aninvaliddescription^", expiresOn);

        result.Should().BeError(ErrorCode.Validation, Resources.ApiKeyKeep_InvalidDescription);
    }

    [Fact]
    public void WhenSetParametersAndExpiresOnIsLessThanMinimumPeriod_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow;
        var result = _apiKey.SetParameters("adescription", expiresOn);

        result.Should().BeError(ErrorCode.Validation, Resources.APIKeyRoot_ExpiresOnTooSoon);
    }

    [Fact]
    public void WhenSetParametersWithNoExpiresOn_ThenAssigns()
    {
        var expires = DateTime.UtcNow.AddDays(1);
        _apiKey.SetParameters("adescription", expires);

        _apiKey.ApiKey.Value.Token.Should().Be("atoken");
        _apiKey.ApiKey.Value.KeyHash.Should().Be("akeyhash");
        _apiKey.Description.Should().BeSome("adescription");
        _apiKey.ExpiresOn.Should().BeSome(expires);
        _apiKey.UserId.Should().Be("auserid".ToId());
        _apiKey.Events.Last().Should().BeOfType<ParametersChanged>();
    }

    [Fact]
    public void WhenSetParametersWithExpiresOn_ThenAssigns()
    {
        var expiresOn = DateTime.UtcNow.Add(Validations.ApiKey.MinimumExpiryPeriod).AddMinutes(1);

        _apiKey.SetParameters("adescription", expiresOn);

        _apiKey.ApiKey.Value.Token.Should().Be("atoken");
        _apiKey.ApiKey.Value.KeyHash.Should().Be("akeyhash");
        _apiKey.Description.Should().BeSome("adescription");
        _apiKey.ExpiresOn.Should().BeSome(expiresOn);
        _apiKey.UserId.Should().Be("auserid".ToId());
        _apiKey.Events.Last().Should().BeOfType<ParametersChanged>();
    }

    [Fact]
    public void WhenVerifyKeyAndKeyIsInvalid_ThenReturnsFalse()
    {
        _apiKeyHasherService.Setup(khs => khs.ValidateKey(It.IsAny<string>()))
            .Returns(false);

        var result = _apiKey.VerifyKey("akey");

        result.Should().BeError(ErrorCode.Validation, Resources.ApiKeyKeep_InvalidKey);
    }

    [Fact]
    public void WhenVerifyKey_ThenReturnsTrue()
    {
        var result = _apiKey.VerifyKey("akey");

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _apiKey.Events.Last().Should().BeOfType<KeyVerified>();
    }

    [Fact]
    public void WhenDeleteAndNotOwner_ThenReturnsError()
    {
        var result = _apiKey.Delete("anotheruserid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.ApiKeyRoot_NotOwner);
    }

    [Fact]
    public void WhenDeleteAndOwner_ThenDeletes()
    {
        var result = _apiKey.Delete("auserid".ToId());

        result.Should().BeSuccess();
        _apiKey.Events.Last().Should().BeOfType<Deleted>();
    }

    [Fact]
    public void WhenForceExpireAndNotOwner_ThenReturnsError()
    {
        var result = _apiKey.ForceExpire("anotheruserid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.ApiKeyRoot_NotOwner);
    }

    [Fact]
    public void WhenForceExpireAndExpired_ThenDoesNothing()
    {
#if TESTINGONLY
        _apiKey.TestingOnly_Expire();
#endif

        var result = _apiKey.ForceExpire("auserid".ToId());

        result.Should().BeSuccess();
        _apiKey.Events.Last().Should().NotBeOfType<Expired>();
    }

    [Fact]
    public void WhenForceExpireAndNotExpired_ThenExpires()
    {
        var result = _apiKey.ForceExpire("auserid".ToId());

        result.Should().BeSuccess();
        _apiKey.Events.Last().Should().BeOfType<Expired>();
    }
}