using Common;
using Common.Extensions;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OAuth2ClientSecretsSpec
{
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly OAuth2ClientSecrets _secrets;

    public OAuth2ClientSecretsSpec()
    {
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(phs => phs.HashPassword(It.IsAny<string>()))
            .Returns((string value) => value);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _secrets = new OAuth2ClientSecrets();
    }

    [Fact]
    public void WhenEnsureInvariants_ThenSucceeds()
    {
        var result = _secrets.EnsureInvariants();

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenAdd_ThenSucceeds()
    {
        var secret1 = OAuth2ClientSecret.Create("asecrethash1", "1234", Optional<DateTime>.None).Value;
        var secret2 = OAuth2ClientSecret.Create("asecrethash2", "1234", Optional<DateTime>.None).Value;
        var secret3 = OAuth2ClientSecret.Create("asecrethash3", "1234", Optional<DateTime>.None).Value;

        _secrets.Add(secret1);
        _secrets.Add(secret2);
        _secrets.Add(secret3);

        _secrets.Count.Should().Be(3);
        _secrets[0].Should().Be(secret1);
        _secrets[1].Should().Be(secret2);
        _secrets[2].Should().Be(secret3);
    }

    [Fact]
    public void WhenVerifyAndNoSecrets_ThenReturnsError()
    {
        var result = _secrets.Verify(_passwordHasherService.Object, "asecret");

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.OAuth2ClientSecrets_UnknownSecret);
    }

    [Fact]
    public void WhenVerifyAndNoMatch_ThenReturnsError()
    {
        var secret1 = OAuth2ClientSecret.Create("asecrethash1", "1234", Optional<DateTime>.None).Value;
        var secret2 = OAuth2ClientSecret.Create("asecrethash2", "1234", Optional<DateTime>.None).Value;
        var secret3 = OAuth2ClientSecret.Create("asecrethash3", "1234", Optional<DateTime>.None).Value;

        _secrets.Add(secret1);
        _secrets.Add(secret2);
        _secrets.Add(secret3);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var result = _secrets.Verify(_passwordHasherService.Object, "asecret");

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.OAuth2ClientSecrets_UnknownSecret);
    }

    [Fact]
    public void WhenVerifyAndMatchIsExpired_ThenReturnsError()
    {
        var secret = OAuth2ClientSecret.Create("asecrethash1", "1234", DateTime.UtcNow.SubtractSeconds(1)).Value;

        _secrets.Add(secret);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = _secrets.Verify(_passwordHasherService.Object, "asecret");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OAuth2ClientSecrets_SecretExpired);
    }
}