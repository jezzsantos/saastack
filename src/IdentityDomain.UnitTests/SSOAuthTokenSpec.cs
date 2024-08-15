using Common;
using Domain.Services.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class SSOAuthTokenSpec
{
    [Fact]
    public void WhenCreateAndValueIsEmpty_ThenReturnsError()
    {
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string _) => "anencryptedvalue");

        var result = SSOAuthToken.Create(SSOAuthTokenType.AccessToken, string.Empty, null, encryptionService.Object);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreate_ThenReturns()
    {
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string _) => "anencryptedvalue");
        var expiresOn = DateTime.UtcNow;
        var result = SSOAuthToken.Create(SSOAuthTokenType.AccessToken, "atoken", expiresOn, encryptionService.Object);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(SSOAuthTokenType.AccessToken);
        result.Value.EncryptedValue.Should().Be("anencryptedvalue");
        result.Value.ExpiresOn.Should().Be(expiresOn);
    }
}