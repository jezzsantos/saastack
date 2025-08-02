using Common;
using Domain.Services.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class AuthTokenSpec
{
    [Fact]
    public void WhenCreateAndValueIsEmpty_ThenReturnsError()
    {
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string _) => "anencryptedvalue");

        var result = AuthToken.Create(AuthTokenType.AccessToken, string.Empty, null, encryptionService.Object);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithPlainValue_ThenReturns()
    {
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string _) => "anencryptedvalue");
        var expiresOn = DateTime.UtcNow;
        var result = AuthToken.Create(AuthTokenType.AccessToken, "eyJtoken", expiresOn, encryptionService.Object);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(AuthTokenType.AccessToken);
        result.Value.EncryptedValue.Should().Be("anencryptedvalue");
        result.Value.ExpiresOn.Should().Be(expiresOn);
        encryptionService.Verify(es => es.Encrypt("eyJtoken"));
    }

    [Fact]
    public void WhenCreateAndPlainValueButNotValid_ThenReturnsError()
    {
        var encryptionService = new Mock<IEncryptionService>();
        var expiresOn = DateTime.UtcNow;
        var result = AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", expiresOn,
            encryptionService.Object);

        result.Should().BeError(ErrorCode.Validation, Resources.AuthToken_InvalidPlainValue);
    }

    [Fact]
    public void WhenCreateWithEncryptedValue_ThenReturns()
    {
        var expiresOn = DateTime.UtcNow;
        var result = AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", expiresOn);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(AuthTokenType.AccessToken);
        result.Value.EncryptedValue.Should().Be("anencryptedvalue");
        result.Value.ExpiresOn.Should().Be(expiresOn);
    }

    [Fact]
    public void WhenCreateAndWithEncryptedValueButNotValid_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow;
        var result = AuthToken.Create(AuthTokenType.AccessToken, "eyJtoken", expiresOn);

        result.Should().BeError(ErrorCode.Validation, Resources.AuthToken_InvalidEncryptedValue);
    }

    [Fact]
    public void WhenCreateWithAuthTokenEvent_ThenReturns()
    {
        var expiresOn = DateTime.UtcNow;
        var token = new Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken
        {
            EncryptedValue = "anencryptedvalue",
            Type = AuthTokenType.AccessToken.ToString(),
            ExpiresOn = expiresOn
        };

        var result = AuthToken.Create(token);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(AuthTokenType.AccessToken);
        result.Value.EncryptedValue.Should().Be("anencryptedvalue");
        result.Value.ExpiresOn.Should().Be(expiresOn);
    }
}