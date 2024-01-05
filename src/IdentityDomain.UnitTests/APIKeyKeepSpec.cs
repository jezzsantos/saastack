using Common;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class APIKeyKeepSpec
{
    private readonly Mock<IAPIKeyHasherService> _apiKeyHasherService;

    public APIKeyKeepSpec()
    {
        _apiKeyHasherService = new Mock<IAPIKeyHasherService>();
        _apiKeyHasherService.Setup(khs => khs.ValidateAPIKeyHash(It.IsAny<string>()))
            .Returns(true);
        _apiKeyHasherService.Setup(khs => khs.ValidateKey(It.IsAny<string>()))
            .Returns(true);
    }

    [Fact]
    public void WhenCreateAndTokenIsEmpty_ThenReturnsError()
    {
        var result = APIKeyKeep.Create(_apiKeyHasherService.Object, string.Empty, "akeyhash");

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateAndKeyHashIsEmpty_ThenReturnsError()
    {
        var result = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateAndInvalidKeyHash_ThenReturnsError()
    {
        _apiKeyHasherService.Setup(khs => khs.ValidateAPIKeyHash(It.IsAny<string>()))
            .Returns(false);

        var result = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash");

        result.Should().BeError(ErrorCode.Validation, Resources.ApiKeyKeep_InvalidKeyHash);
    }

    [Fact]
    public void WhenCreate_ThenReturns()
    {
        var result = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash");

        result.Should().BeSuccess();
        result.Value.Token.Should().Be("atoken");
        result.Value.KeyHash.Should().Be("akeyhash");
    }

    [Fact]
    public void WhenChangeKeyAndTokenIsEmpty_ThenReturnsError()
    {
        var keep = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash").Value;

        var result = keep.ChangeKey(_apiKeyHasherService.Object, string.Empty, "akeyhash");

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenChangeKeyAndKeyHashIsEmpty_ThenReturnsError()
    {
        var keep = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash").Value;

        var result = keep.ChangeKey(_apiKeyHasherService.Object, "atoken", string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenChangeKeyAndKeyHashIsInvalid_ThenReturnsError()
    {
        _apiKeyHasherService.Setup(khs => khs.ValidateAPIKeyHash(It.IsAny<string>()))
            .Returns(true);
        var keep = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash").Value;
        _apiKeyHasherService.Setup(khs => khs.ValidateAPIKeyHash(It.IsAny<string>()))
            .Returns(false);

        var result = keep.ChangeKey(_apiKeyHasherService.Object, "atoken", "akeyhash");

        result.Should().BeError(ErrorCode.Validation, Resources.ApiKeyKeep_InvalidKeyHash);
    }

    [Fact]
    public void WhenVerifyAndKeyIsEmpty_ThenReturnsError()
    {
        var keep = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash").Value;

        var result = keep.Verify(_apiKeyHasherService.Object, string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenVerifyAndKeyIsInvalid_ThenReturnsError()
    {
        _apiKeyHasherService.Setup(khs => khs.ValidateKey(It.IsAny<string>()))
            .Returns(false);

        var keep = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash").Value;

        var result = keep.Verify(_apiKeyHasherService.Object, "akey");

        result.Should().BeError(ErrorCode.Validation, Resources.ApiKeyKeep_InvalidKey);
    }

    [Fact]
    public void WhenVerifyAndKeyHashHasNoValue_ThenReturnsError()
    {
        var keep = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash").Value;
#if TESTINGONLY
        keep.TestingOnly_ResetKeyHash();
#endif

        var result = keep.Verify(_apiKeyHasherService.Object, "akey");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.ApiKeyKeep_NoApiKeyHash);
    }

    [Fact]
    public void WhenVerify_ThenReturnsVerified()
    {
        _apiKeyHasherService.Setup(khs => khs.VerifyAPIKey(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        var keep = APIKeyKeep.Create(_apiKeyHasherService.Object, "atoken", "akeyhash").Value;

        var result = keep.Verify(_apiKeyHasherService.Object, "akey");

        result.Should().BeSuccess();
        _apiKeyHasherService.Verify(khs => khs.VerifyAPIKey("akey", "akeyhash"));
    }
}