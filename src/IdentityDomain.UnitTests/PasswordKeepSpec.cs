using Common;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class PasswordKeepSpec
{
    private const string Token = "5n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
    private readonly Mock<IPasswordHasherService> _passwordHasherService;

    public PasswordKeepSpec()
    {
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(es => es.HashPassword(It.IsAny<string>()))
            .Returns("apasswordhash");
        _passwordHasherService.Setup(es => es.ValidatePasswordHash("apasswordhash"))
            .Returns(true);
        _passwordHasherService.Setup(es => es.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
    }

    [Fact]
    public void WhenConstructed_ThenPropertiesAssigned()
    {
        var password = PasswordKeep.Create().Value;

        password.PasswordHash.Should().BeNone();
        password.Token.Should().BeNone();
        password.TokenExpiresUtc.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedWithEmptyPasswordHash_ThenReturnsError()
    {
        var result = PasswordKeep.Create(_passwordHasherService.Object, string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenConstructedWithInvalidPasswordHash_ThenReturnsError()
    {
        var result = PasswordKeep.Create(_passwordHasherService.Object, "aninvalidpasswordhash");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordKeep_InvalidPasswordHash);
    }

    [Fact]
    public void WhenConstructedWithHash_ThenPropertiesAssigned()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        password.PasswordHash.Should().Be("apasswordhash");
        password.Token.Should().BeNone();
        password.TokenExpiresUtc.Should().BeNone();
    }

    [Fact]
    public void WhenInitiatePasswordResetAndNoPasswordSet_ThenReturnsError()
    {
        var password = PasswordKeep.Create().Value;

        var result = password.InitiatePasswordReset(Token);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordKeep_NoPasswordHash);
    }

    [Fact]
    public void WhenInitiatePasswordReset_ThenCreatesResetToken()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        password = password.InitiatePasswordReset(Token).Value;

        password.PasswordHash.Should().Be("apasswordhash");
        password.Token.Should().Be(Token);
        password.TokenExpiresUtc.Should().BeNear(DateTime.UtcNow.Add(PasswordKeep.DefaultResetExpiry));
    }

    [Fact]
    public void WhenInitiatePasswordResetTwice_ThenCreatesNewResetToken()
    {
        const string token1 = "5n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
        const string token2 = "7n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        password = password.InitiatePasswordReset(token1).Value;
        password = password.InitiatePasswordReset(token2).Value;

        password.PasswordHash.Should().Be("apasswordhash");
        password.Token.Should().Be(token2);
        password.TokenExpiresUtc.Should().BeNear(DateTime.UtcNow.Add(PasswordKeep.DefaultResetExpiry));
    }

    [Fact]
    public void WhenVerifyAndEmptyPassword_ThenReturnsError()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        var result = password.Verify(_passwordHasherService.Object, string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenVerifyAndInvalidPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(ph => ph.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false);
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        var result = password.Verify(_passwordHasherService.Object, "apassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordKeep_InvalidPassword);
        _passwordHasherService.Verify(ph => ph.ValidatePassword("apassword", false));
    }

    [Fact]
    public void WhenVerifyAndNoPasswordSet_ThenReturnsError()
    {
        var password = PasswordKeep.Create().Value;

        var result = password.Verify(_passwordHasherService.Object, "apassword");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordKeep_NoPasswordHash);
    }

    [Fact]
    public void WhenVerifyAndNotMatchesHash_ThenReturnsFalse()
    {
        _passwordHasherService.Setup(es => es.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        var result = password.Verify(_passwordHasherService.Object, "anotherpassword");

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        _passwordHasherService.Verify(es => es.VerifyPassword("anotherpassword", "apasswordhash"));
    }

    [Fact]
    public void WhenVerifyAndMatchesHash_ThenReturnsTrue()
    {
        _passwordHasherService.Setup(es => es.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        var result = password.Verify(_passwordHasherService.Object, "apassword");

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _passwordHasherService.Verify(es => es.VerifyPassword("apassword", "apasswordhash"));
    }

    [Fact]
    public void WhenConfirmResetWithEmptyToken_ThenReturnsError()
    {
        var password = PasswordKeep.Create().Value;

        var result = password.ConfirmReset(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenConfirmResetWithInvalidToken_ThenReturnsError()
    {
        var password = PasswordKeep.Create().Value;

        var result = password.ConfirmReset("aninvalidtoken");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordKeep_InvalidToken);
    }

    [Fact]
    public void WhenConfirmResetAndTokensNotMatch_ThenReturnsError()
    {
        const string token1 = "5n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
        const string token2 = "7n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;
        password = password.InitiatePasswordReset(token1).Value;

        var result = password.ConfirmReset(token2);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordKeep_TokensNotMatch);
    }

    [Fact]
    public void WhenConfirmResetAndTokenExpired_ThenReturnsError()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;
        password = password.InitiatePasswordReset(Token).Value;
#if TESTINGONLY
        password = password.TestingOnly_ExpireToken();
#endif

        var result = password.ConfirmReset(Token);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PasswordKeep_TokenExpired);
    }

    [Fact]
    public void WhenResetPasswordAndEmptyPasswordHash_ThenReturnsError()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        var result = password.ResetPassword(_passwordHasherService.Object, Token, string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenResetPasswordAndTokenInvalid_ThenReturnsError()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;

        var result = password.ResetPassword(_passwordHasherService.Object, "aninvalidtoken", "apassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordKeep_InvalidToken);
    }

    [Fact]
    public void WhenResetPasswordAndPasswordHashInvalid_ThenReturnsError()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;
        _passwordHasherService.Setup(ph => ph.ValidatePasswordHash(It.IsAny<string>()))
            .Returns(false);

        var result = password.ResetPassword(_passwordHasherService.Object, Token, "aninvalidpasswordhash");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordKeep_InvalidPasswordHash);
    }

    [Fact]
    public void WhenResetPasswordAndTokenNotMatch_ThenReturnsError()
    {
        const string token1 = "5n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
        const string token2 = "7n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;
        password = password.InitiatePasswordReset(token1).Value;

        var result = password.ResetPassword(_passwordHasherService.Object, token2, "apasswordhash");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordKeep_TokensNotMatch);
    }

    [Fact]
    public void WhenResetPasswordAndTokenExpired_ThenReturnsError()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value;
        password = password.InitiatePasswordReset(Token).Value;
#if TESTINGONLY
        password = password.TestingOnly_ExpireToken();
#endif

        var result = password.ResetPassword(_passwordHasherService.Object, Token, "apasswordhash");

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PasswordKeep_TokenExpired);
    }

    [Fact]
    public void WhenResetPasswordAndNoPasswordSet_ThenReturnsError()
    {
        var password = PasswordKeep.Create().Value;

        var result = password.ResetPassword(_passwordHasherService.Object, Token, "apasswordhash");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordKeep_NoPasswordHash);
    }

    [Fact]
    public void WhenResetPassword_ThenReturnsNewPassword()
    {
        var password = PasswordKeep.Create(_passwordHasherService.Object, "apasswordhash").Value
            .InitiatePasswordReset(Token).Value;

        password = password.ResetPassword(_passwordHasherService.Object, password.Token, "apasswordhash").Value;

        password.PasswordHash.Should().Be("apasswordhash");
        password.Token.Should().BeNone();
        password.TokenExpiresUtc.Should().BeNone();
    }
}