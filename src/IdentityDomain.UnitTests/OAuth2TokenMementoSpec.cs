using Common;
using Domain.Services.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OAuth2TokenMementoSpec
{
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<ITokensService> _tokensService;

    public OAuth2TokenMementoSpec()
    {
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateTokenDigest(It.IsAny<string>()))
            .Returns("adigestvalue");
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns("anencryptedvalue");
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns("eyJadecryptedvalue");
    }

    [Fact]
    public void WhenCreateWithDigestValue_ThenReturnsMemento()
    {
        var expiresOn = DateTime.UtcNow;

        var result = OAuth2TokenMemento.Create(AuthTokenType.AccessToken, "adigestvalue", expiresOn);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(AuthTokenType.AccessToken);
        result.Value.DigestValue.Should().Be("adigestvalue");
        result.Value.ExpiresOn.Should().Be(expiresOn);
    }

    [Fact]
    public void WhenCreateWithDigestValueValueButNotValid_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow;

        var result = OAuth2TokenMemento.Create(AuthTokenType.AccessToken, "eyJtoken", expiresOn);

        result.Should().BeError(ErrorCode.Validation, Resources.OAuth2TokenMemento_InvalidDigestValue);
    }

    [Fact]
    public void WhenCreateWithPlainValue_ThenDigestsAndReturnsMemento()
    {
        var expiresOn = DateTime.UtcNow;

        var result = OAuth2TokenMemento.Create(AuthTokenType.AccessToken, "eyJtoken", expiresOn, _tokensService.Object);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(AuthTokenType.AccessToken);
        result.Value.DigestValue.Should().Be("adigestvalue");
        result.Value.ExpiresOn.Should().Be(expiresOn);
        _tokensService.Verify(ts => ts.CreateTokenDigest("eyJtoken"));
    }

    [Fact]
    public void WhenCreateWithPlainValueValueButNotValid_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow;

        var result =
            OAuth2TokenMemento.Create(AuthTokenType.AccessToken, "adigestvalue", expiresOn, _tokensService.Object);

        result.Should().BeError(ErrorCode.Validation, Resources.OAuth2TokenMemento_InvalidPlainValue);
    }

    [Fact]
    public void WhenCreateWithAuthToken_ThenDecryptsAndDigestsAndReturnsMemento()
    {
        var expiresOn = DateTime.UtcNow;
        var token = AuthToken.Create(AuthTokenType.AccessToken, "eyJtoken", expiresOn, _encryptionService.Object).Value;

        var result = OAuth2TokenMemento.Create(token, _encryptionService.Object, _tokensService.Object);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(AuthTokenType.AccessToken);
        result.Value.DigestValue.Should().Be("adigestvalue");
        result.Value.ExpiresOn.Should().Be(expiresOn);
        _encryptionService.Verify(es => es.Decrypt("anencryptedvalue"));
        _tokensService.Verify(ts => ts.CreateTokenDigest("eyJadecryptedvalue"));
    }
}