using Domain.Interfaces.Validations;
using FluentAssertions;
using IdentityInfrastructure.DomainServices;
using Xunit;

namespace IdentityInfrastructure.UnitTests.DomainServices;

[Trait("Category", "Unit")]
public class PasswordHasherServiceSpec
{
    private readonly PasswordHasherService _service = new();

    [Fact]
    public void WhenVerifyPasswordWithEmptyHash_ThenReturnsFalse()
    {
        var result = _service.VerifyPassword("apassword", string.Empty);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyPasswordWithEmptyPassword_ThenReturnsFalse()
    {
        var result = _service.VerifyPassword(string.Empty, "awrongpasswordhash");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyPasswordWithInCorrectHash_ThenReturnsFalse()
    {
        var result = _service.VerifyPassword("apassword", "awrongpasswordhash");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyPasswordWithDifferentHash_ThenReturnsFalse()
    {
        var hash = _service.HashPassword("apassword1");

        var result = _service.VerifyPassword("apassword2", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenVerifyPasswordWithCorrectHash_ThenReturnsTrue()
    {
        var hash = _service.HashPassword("apassword");

        var result = _service.VerifyPassword("apassword", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHashPasswordWithShortestPassword_ThenReturnsHash()
    {
        var password = new string('a', CommonValidations.Passwords.Password.MinLength);
        var result = _service.HashPassword(password);

        result.Should().NotBeNullOrEmpty();
        _service.ValidatePasswordHash(result).Should().BeTrue();
    }

    [Fact]
    public void WhenHashPasswordWithLongestPassword_ThenReturnsHash()
    {
        var password = new string('a', CommonValidations.Passwords.Password.MaxLength);
        var result = _service.HashPassword(password);

        result.Should().NotBeNullOrEmpty();
        _service.ValidatePasswordHash(result).Should().BeTrue();
    }
}