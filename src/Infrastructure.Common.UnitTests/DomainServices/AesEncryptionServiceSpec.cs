using FluentAssertions;
using Infrastructure.Common.DomainServices;
using Xunit;

namespace Infrastructure.Common.UnitTests.DomainServices;

[Trait("Category", "Unit")]
public class AesEncryptionServiceSpec
{
    private readonly string _secret;
    private readonly AesEncryptionService _service;

    public AesEncryptionServiceSpec()
    {
#if TESTINGONLY
        _secret = AesEncryptionService.GenerateAesSecret();
#else
        _secret = string.Empty;
#endif

        _service = new AesEncryptionService(_secret);
    }

    [Fact]
    public void WhenCreateAesSecret_ThenReturnsSecret()
    {
        _secret.Should().NotBeEmpty();
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