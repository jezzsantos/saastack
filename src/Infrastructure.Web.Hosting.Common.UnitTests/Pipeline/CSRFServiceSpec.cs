using Application.Interfaces.Services;
using Common;
using FluentAssertions;
using Infrastructure.Common.DomainServices;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Pipeline;

[Trait("Category", "Unit")]
public class CSRFServiceSpec
{
    private readonly AesEncryptionService _encryptionService;
    private readonly CSRFService _service;

    public CSRFServiceSpec()
    {
        var settings = new Mock<IHostSettings>();
        settings.Setup(s => s.GetWebsiteHostCSRFSigningSecret())
            .Returns("asecret");
#if TESTINGONLY
        _encryptionService = new AesEncryptionService(AesEncryptionService.CreateAesSecret());
#endif

        _service = new CSRFService(settings.Object, _encryptionService);
    }

    [Fact]
    public void WhenCreateTokensWithNoUserId_ThenReturnsPair()
    {
        var result = _service.CreateTokens(Optional<string>.None);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNone();
        result.Signature.Should().NotBeNone();
    }

    [Fact]
    public void WhenCreateTokensWithUserId_ThenReturnsPair()
    {
        var result = _service.CreateTokens("auserid");

        result.Should().NotBeNull();
        result.Token.Should().NotBeNone();
        result.Signature.Should().NotBeNone();
    }

    [Fact]
    public void WhenVerifyTokensAndTokenIsNone_ThenReturnsFalse()
    {
        var result = _service.VerifyTokens(Optional<string>.None, "asignature", Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyTokensAndSignatureIsNone_ThenReturnsFalse()
    {
        var result = _service.VerifyTokens("atoken", Optional<string>.None, Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyTokensAndSignatureIsInvalidForNoUser_ThenReturnsFalse()
    {
        var token = CSRFTokenPair.CreateTokens(_encryptionService, "asecret", Optional<string>.None).Token;

        var result = _service.VerifyTokens(token, "awrongsignature", Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyTokensAndSignatureIsInvalidForUser_ThenReturnsFalse()
    {
        var token = CSRFTokenPair.CreateTokens(_encryptionService, "asecret", "auserid").Token;

        var result = _service.VerifyTokens(token, "awrongsignature", "auserid");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyTokensAndSignatureIsValidForUserButNotForNoUser_ThenReturnsFalse()
    {
        var pair = CSRFTokenPair.CreateTokens(_encryptionService, "asecret", "auserid");
        var token = pair.Token;
        var signature = pair.Signature;

        var result = _service.VerifyTokens(token, signature, Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyTokensAndSignatureIsValidForNoUserButNotForUser_ThenReturnsFalse()
    {
        var pair = CSRFTokenPair.CreateTokens(_encryptionService, "asecret", Optional<string>.None);
        var token = pair.Token;
        var signature = pair.Signature;

        var result = _service.VerifyTokens(token, signature, "auserid");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyTokensAndSignatureIsValidForNoUser_ThenReturnsTrue()
    {
        var pair = CSRFTokenPair.CreateTokens(_encryptionService, "asecret", Optional<string>.None);
        var token = pair.Token;
        var signature = pair.Signature;

        var result = _service.VerifyTokens(token, signature, Optional<string>.None);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenVerifyTokensAndSignatureIsValidForUser_ThenReturnsTrue()
    {
        var pair = CSRFTokenPair.CreateTokens(_encryptionService, "asecret", "auserid");
        var token = pair.Token;
        var signature = pair.Signature;

        var result = _service.VerifyTokens(token, signature, "auserid");

        result.Should().BeTrue();
    }
}