using Common;
using FluentAssertions;
using Infrastructure.Common.DomainServices;
using Infrastructure.Web.Hosting.Common.Pipeline;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Pipeline;

[Trait("Category", "Unit")]
public class CSRFTokenPairSpec
{
    private const string HmacSecret = "asecret";
    private readonly AesEncryptionService _encryptionService;

    public CSRFTokenPairSpec()
    {
#if TESTINGONLY
        _encryptionService =
            new AesEncryptionService(AesEncryptionService.CreateAesSecret());
#endif
    }

    [Fact]
    public void WhenFromTokens_ThenReturnsPair()
    {
        var result = CSRFTokenPair.FromTokens("atoken", "asignature");

        result.Token.Should().Be("atoken");
        result.Signature.Should().Be("asignature");
    }

    [Fact]
    public void WhenFromTokensWithNone_ThenReturnsPair()
    {
        var result = CSRFTokenPair.FromTokens(Optional<string>.None, Optional<string>.None);

        result.Token.Should().BeNone();
        result.Signature.Should().BeNone();
    }

    [Fact]
    public void WhenIsValidAndTokenIsNone_ThenReturnsFalse()
    {
        var result = CSRFTokenPair.FromTokens(Optional<string>.None, "asignature")
            .IsValid(_encryptionService, HmacSecret, Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidAndSignatureIsNone_ThenReturnsFalse()
    {
        var result = CSRFTokenPair.FromTokens("atoken", Optional<string>.None)
            .IsValid(_encryptionService, HmacSecret, Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidAndSignatureAreNotMatched_ThenReturnsFalse()
    {
        var tokens1 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid1");
        var tokens2 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid2");

        var result = CSRFTokenPair.FromTokens(tokens1.Token, tokens2.Signature)
            .IsValid(_encryptionService, HmacSecret, Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidAndSignatureAreMatched_ThenReturnsTrue()
    {
        var tokens = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");

        var result = CSRFTokenPair.FromTokens(tokens.Token, tokens.Signature)
            .IsValid(_encryptionService, HmacSecret, "auserid");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateTokensAndHmacSecretIsEmpty_ThenThrows()
    {
        FluentActions.Invoking(() => CSRFTokenPair.CreateTokens(_encryptionService, string.Empty, "auserid"))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WhenCreateTokensAndUserIdIsNone_ThenReturnsTokens()
    {
        var result = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);

        result.Token.Should().NotBeNone();
        result.Signature.Should().NotBeNone();
    }

    [Fact]
    public void WhenCreateTokensAndUserId_ThenReturnsTokens()
    {
        var result = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");

        result.Token.Should().NotBeNone();
        result.Signature.Should().NotBeNone();
    }

    [Fact]
    public void WhenCreateTokensAndUserIdIsNone_ThenSignatureAndTokenCannotBeSame()
    {
        var result = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);

        result.Token.Should().NotBe(result.Signature);
    }

    [Fact]
    public void WhenCreateTokensAndUserId_ThenSignatureAndTokenCannotBeSame()
    {
        var result = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");

        result.Token.Should().NotBe(result.Signature);
    }

    [Fact]
    public void WhenCreateTokensAndUserIdIsNone_ThenReturnedTokensCannotBeTheNullUserId()
    {
        var result = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);

        result.Token.Should().NotBe(CSRFTokenPair.NullUserIdTokenValue);
        result.Signature.Should().NotBeNone();
    }

    [Fact]
    public void WhenCreateTokensAndUserId_ThenReturnedTokensCannotBeTheUserId()
    {
        var result = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");

        result.Token.Should().NotBe("auserid");
        result.Signature.Should().NotBeNone();
    }

    [Fact]
    public void WhenCreateTokensMultipleTimesForSameUserId_ThenTokensMustBeUniqueWithSameSignature()
    {
        var result1 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");
        var result2 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");

        result1.Token.Should().NotBe(result2.Token);
        result1.Signature.Should().Be(result2.Signature);
    }

    [Fact]
    public void WhenCreateTokensMultipleTimesForSameNoneUserId_ThenTokensMustBeUniqueWithSameSignature()
    {
        var result1 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);
        var result2 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);

        result1.Token.Should().NotBe(result2.Token);
        result1.Signature.Should().Be(result2.Signature);
    }

    [Fact]
    public void WhenIsValidWithNoneUserIdAndTokenIsForNoneUserId_ThenReturnsTrue()
    {
        var tokens = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);

        var result = CSRFTokenPair.FromTokens(tokens.Token, tokens.Signature)
            .IsValid(_encryptionService, HmacSecret, Optional<string>.None);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidWithUserIdAndTokenIsForSameUserId_ThenReturnsTrue()
    {
        var tokens = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");

        var result = CSRFTokenPair.FromTokens(tokens.Token, tokens.Signature)
            .IsValid(_encryptionService, HmacSecret, "auserid");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidWithOneUserIdAndTokenIsForDifferentUserId_ThenReturnsFalse()
    {
        var tokens = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid1");

        var result = CSRFTokenPair.FromTokens(tokens.Token, tokens.Signature)
            .IsValid(_encryptionService, HmacSecret, "auserid2");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWithOneUserIdAndTokenIsForNoneUserId_ThenReturnsFalse()
    {
        var tokens = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid1");

        var result = CSRFTokenPair.FromTokens(tokens.Token, tokens.Signature)
            .IsValid(_encryptionService, HmacSecret, Optional<string>.None);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWithNoneUserIdAndTokenIsForOneUserId_ThenReturnsFalse()
    {
        var tokens = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, null);

        var result = CSRFTokenPair.FromTokens(tokens.Token, tokens.Signature)
            .IsValid(_encryptionService, HmacSecret, "auserid");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWithNoneUserIdAndDifferentTokensForNoneUserId_ThenReturnsTrue()
    {
        var tokens1 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);
        var tokens2 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, Optional<string>.None);

        var result1 = CSRFTokenPair.FromTokens(tokens1.Token, tokens2.Signature)
            .IsValid(_encryptionService, HmacSecret, Optional<string>.None);
        var result2 = CSRFTokenPair.FromTokens(tokens2.Token, tokens1.Signature)
            .IsValid(_encryptionService, HmacSecret, Optional<string>.None);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    [Fact]
    public void WhenIsValidWithOneUserIdAndDifferentTokensForOneUserId_ThenReturnsTrue()
    {
        var tokens1 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");
        var tokens2 = CSRFTokenPair.CreateTokens(_encryptionService, HmacSecret, "auserid");

        var result1 = CSRFTokenPair.FromTokens(tokens1.Token, tokens2.Signature)
            .IsValid(_encryptionService, HmacSecret, "auserid");
        var result2 = CSRFTokenPair.FromTokens(tokens2.Token, tokens1.Signature)
            .IsValid(_encryptionService, HmacSecret, "auserid");

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }
}