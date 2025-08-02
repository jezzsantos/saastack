using Common;
using Common.Extensions;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OAuth2ClientSecretSpec
{
    private readonly Mock<IPasswordHasherService> _passwordHasherService;

    public OAuth2ClientSecretSpec()
    {
        _passwordHasherService = new Mock<IPasswordHasherService>();
    }

    [Fact]
    public void WhenCreateWithHashAndEmptyHash_ThenReturnsError()
    {
        var result = OAuth2ClientSecret.Create(string.Empty, "1234", Optional<DateTime>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.OAuth2ClientSecret_InvalidSecretHash);
    }

    [Fact]
    public void WhenCreateWithHashAndEmptyFirstFour_ThenReturnsError()
    {
        var result = OAuth2ClientSecret.Create("asecrethash", string.Empty, Optional<DateTime>.None);

        result.Should().BeError(ErrorCode.Validation, Resources.OAuth2ClientSecret_InvalidFirstFour);
    }

    [Fact]
    public void WhenCreateWithHashAndNoExpiry_ThenCreates()
    {
        var result = OAuth2ClientSecret.Create("asecrethash", "1234", Optional<DateTime>.None);

        result.Should().BeSuccess();
        result.Value.SecretHash.Should().Be("asecrethash");
        result.Value.FirstFour.Should().Be("1234");
        result.Value.ExpiresOn.Should().BeNone();
        result.Value.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void WhenCreateWithHashAndSomeExpiryInPast_ThenCreates()
    {
        var expiresOn = DateTime.UtcNow.SubtractSeconds(1);
        var result = OAuth2ClientSecret.Create("asecrethash", "1234", expiresOn);

        result.Should().BeSuccess();
        result.Value.SecretHash.Should().Be("asecrethash");
        result.Value.FirstFour.Should().Be("1234");
        result.Value.ExpiresOn.Should().BeSome(expiresOn);
        result.Value.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateWithHashAndSomeExpiryInFuture_ThenCreates()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        var result = OAuth2ClientSecret.Create("asecrethash", "1234", expiresOn);

        result.Should().BeSuccess();
        result.Value.SecretHash.Should().Be("asecrethash");
        result.Value.FirstFour.Should().Be("1234");
        result.Value.ExpiresOn.Should().BeSome(expiresOn);
        result.Value.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void WhenCreateWithPlainSecretAndNotValid_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        var result = OAuth2ClientSecret.Create(string.Empty, expiresOn, _passwordHasherService.Object);

        result.Should().BeError(ErrorCode.Validation, Resources.OAuth2ClientSecret_InvalidSecret);
    }

    [Fact]
    public void WhenIsMatchAndNotMatches_ThenReturnsFalse()
    {
        var secret = OAuth2ClientSecret.Create("asecrethash", "1234", Optional<DateTime>.None).Value;
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var result = secret.IsMatch(_passwordHasherService.Object, "asecret");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsMatchAndMatches_ThenReturnsTrue()
    {
        var secret = OAuth2ClientSecret.Create("asecrethash", "1234", Optional<DateTime>.None).Value;
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = secret.IsMatch(_passwordHasherService.Object, "asecret");

        result.Should().BeTrue();
    }
}