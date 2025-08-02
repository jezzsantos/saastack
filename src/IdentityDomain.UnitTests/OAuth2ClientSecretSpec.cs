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
    public void WhenCreateAndNoExpiry_ThenCreates()
    {
        var result = OAuth2ClientSecret.Create("asecrethash", Optional<DateTime>.None);

        result.Should().BeSuccess();
        result.Value.SecretHash.Should().Be("asecrethash");
        result.Value.ExpiresOn.Should().BeNone();
        result.Value.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void WhenCreateAndSomeExpiryInPast_ThenCreates()
    {
        var expiresOn = DateTime.UtcNow.SubtractSeconds(1);
        var result = OAuth2ClientSecret.Create("asecrethash", expiresOn);

        result.Should().BeSuccess();
        result.Value.SecretHash.Should().Be("asecrethash");
        result.Value.ExpiresOn.Should().BeSome(expiresOn);
        result.Value.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateAndSomeExpiryInFuture_ThenCreates()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        var result = OAuth2ClientSecret.Create("asecrethash", expiresOn);

        result.Should().BeSuccess();
        result.Value.SecretHash.Should().Be("asecrethash");
        result.Value.ExpiresOn.Should().BeSome(expiresOn);
        result.Value.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void WhenIsMatchAndNotMatches_ThenReturnsFalse()
    {
        var secret = OAuth2ClientSecret.Create("asecrethash", Optional<DateTime>.None).Value;
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var result = secret.IsMatch(_passwordHasherService.Object, "asecret");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsMatchAndMatches_ThenReturnsTrue()
    {
        var secret = OAuth2ClientSecret.Create("asecrethash", Optional<DateTime>.None).Value;
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = secret.IsMatch(_passwordHasherService.Object, "asecret");

        result.Should().BeTrue();
    }
}