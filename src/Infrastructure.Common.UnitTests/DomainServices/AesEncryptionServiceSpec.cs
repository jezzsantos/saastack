#if TESTINGONLY
using FluentAssertions;
using Infrastructure.Common.DomainServices;
using Xunit;

namespace Infrastructure.Common.UnitTests.DomainServices;

[Trait("Category", "Unit")]
public class AesEncryptionServiceSpec
{
    private readonly AesEncryptionService _service;

    public AesEncryptionServiceSpec()
    {
        var secret = AesEncryptionService.CreateAesSecret();

        _service = new AesEncryptionService(secret);
    }

    [Fact]
    public void WhenDecryptAndEncrypted_ThenReturnsPlainText()
    {
        var cipherText = _service.Encrypt("avalue");
        var plainText = _service.Decrypt(cipherText);

        cipherText.Should().NotBe("avalue");
        plainText.Should().NotBe(cipherText);
        plainText.Should().Be("avalue");
    }
}
#endif