using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.PasswordCredentials;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class PasswordCredentialRootSpec
{
    private const string Token = "5n6nA42SQrsO1UIgc7lIVebR6_3CmZwcthUEx3nF2sM";
    private readonly PasswordCredentialRoot _credential;
    private readonly Mock<IEmailAddressService> _emailAddressService;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<ITokensService> _tokensService;

    public PasswordCredentialRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());

        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(phs => phs.HashPassword(It.IsAny<string>()))
            .Returns("apasswordhash");
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.ValidatePasswordHash(It.IsAny<string>()))
            .Returns(true);
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateRegistrationVerificationToken())
            .Returns("averificationtoken");
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s => s.Platform.GetString(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string?)null!);
        settings.Setup(s => s.Platform.GetNumber(It.IsAny<string>(), It.IsAny<double>()))
            .Returns(5);
        _emailAddressService = new Mock<IEmailAddressService>();
        _emailAddressService.Setup(eas => eas.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .ReturnsAsync(true);

        _credential = PasswordCredentialRoot.Create(recorder.Object, idFactory.Object,
            settings.Object, _emailAddressService.Object, _tokensService.Object, _passwordHasherService.Object,
            "auserid".ToId()).Value;
    }

    [Fact]
    public void WhenConstructed_ThenInitialized()
    {
        _credential.UserId.Should().Be("auserid".ToId());
        _credential.Registration.Should().BeNone();
        _credential.Login.IsReset.Should().BeTrue();
        _credential.Password.PasswordHash.Should().BeNone();
    }

    [Fact]
    public void WhenInitiateRegistrationVerificationAndAlreadyVerified_ThenReturnsError()
    {
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();

        var result = _credential.InitiateRegistrationVerification();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PasswordCredentialsRoot_RegistrationVerified);
    }

    [Fact]
    public void WhenInitiateRegistrationVerificationAndNotVerified_ThenInitiates()
    {
        _credential.InitiateRegistrationVerification();

        _credential.VerificationKeep.IsStillVerifying.Should().BeTrue();
        _credential.Events.Last().Should()
            .BeOfType<RegistrationVerificationCreated>();
    }

    [Fact]
    public void WhenSetCredentialAndInvalidPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false);

        var result = _credential.SetPasswordCredential("notavalidpassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordCredentialsRoot_InvalidPassword);
    }

    [Fact]
    public void WhenSetCredentials_ThenSetsCredentials()
    {
        _credential.SetPasswordCredential("apassword");

        _credential.Password.PasswordHash.Should().Be("apasswordhash");
        _credential.Events.Last().Should().BeOfType<CredentialsChanged>();
    }

    [Fact]
    public void WhenSetRegistrationDetails_ThenSetsRegistration()
    {
        _credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("adisplayname").Value);

        _credential.Registration.Value.EmailAddress.Should().Be(EmailAddress.Create("auser@company.com").Value);
        _credential.Registration.Value.Name.Should().Be(PersonDisplayName.Create("adisplayname").Value);
        _credential.Events.Last().Should().BeOfType<RegistrationChanged>();
    }

    [Fact]
    public void WhenVerifyPasswordWithInvalidPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false);

        var result = _credential.VerifyPassword("1WrongPassword!");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordCredentialsRoot_InvalidPassword);
        _credential.Login.FailedPasswordAttempts.Should().Be(0);
        _credential.Login.IsLocked.Should().BeFalse();
        _credential.Login.ToggledLocked.Should().BeFalse();
        _credential.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenVerifyPasswordAndWrongPasswordAndAudit_ThenAuditsFailedLogin()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _credential.SetPasswordCredential("apassword");
        var result = _credential.VerifyPassword("1WrongPassword!");

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        _credential.Login.FailedPasswordAttempts.Should().Be(1);
        _credential.Login.IsLocked.Should().BeFalse();
        _credential.Login.ToggledLocked.Should().BeFalse();
        _credential.Events[1].Should().BeOfType<CredentialsChanged>();
        _credential.Events.Last().Should().BeOfType<PasswordVerified>();
    }

    [Fact]
    public void WhenVerifyPasswordAndAndAudit_ThenResetsLoginMonitor()
    {
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _credential.SetPasswordCredential("apassword");
        var result = _credential.VerifyPassword("apassword");

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _credential.Login.FailedPasswordAttempts.Should().Be(0);
        _credential.Login.IsLocked.Should().BeFalse();
        _credential.Login.ToggledLocked.Should().BeFalse();
        _credential.Events[1].Should().BeOfType<CredentialsChanged>();
        _credential.Events.Last().Should().BeOfType<PasswordVerified>();
        _passwordHasherService.Verify(ph => ph.ValidatePassword("apassword", false));
    }

    [Fact]
    public void WhenVerifyPasswordAndFailsAndLocksAccount_ThenLocksLogin()
    {
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _credential.SetPasswordCredential("apassword");
#if TESTINGONLY
        _credential.TestingOnly_LockAccount("awrongpassword");
#endif
        _credential.Login.FailedPasswordAttempts.Should()
            .Be(Validations.Credentials.Login.DefaultMaxFailedPasswordAttempts);
        _credential.Login.IsLocked.Should().BeTrue();
        _credential.Login.ToggledLocked.Should().BeTrue();
        _credential.Events[1].Should().BeOfType<CredentialsChanged>();
        _credential.Events[2].Should().BeOfType<PasswordVerified>();
        _credential.Events.Last().Should().BeOfType<AccountLocked>();
    }

    [Fact]
    public void WhenVerifyPasswordAndSucceedsAfterCooldown_ThenUnlocksCredentials()
    {
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _credential.SetPasswordCredential("apassword");
#if TESTINGONLY
        _credential.TestingOnly_LockAccount("awrongpassword");
        _credential.TestingOnly_ResetLoginCooldownPeriod();
#endif
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _credential.VerifyPassword("apassword");

        _credential.Login.FailedPasswordAttempts.Should().Be(0);
        _credential.Login.IsLocked.Should().BeFalse();
        _credential.Login.ToggledLocked.Should().BeTrue();
        _credential.Events[1].Should().BeOfType<CredentialsChanged>();
        _credential.Events[2].Should().BeOfType<PasswordVerified>();
        _credential.Events.Last().Should().BeOfType<AccountUnlocked>();
    }

    [Fact]
    public void WhenVerifyRegistrationAndRegistered_ThenReturnsError()
    {
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();

        var result = _credential.VerifyRegistration();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PasswordCredentialsRoot_RegistrationNotVerifying);
    }

    [Fact]
    public void WhenVerifyRegistrationAndExpired_ThenReturnsError()
    {
        _credential.InitiateRegistrationVerification();
#if TESTINGONLY
        _credential.TestingOnly_ExpireRegistrationVerification();
#endif

        var result = _credential.VerifyRegistration();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PasswordCredentialsRoot_RegistrationVerifyingExpired);
    }

    [Fact]
    public void WhenVerifyRegistration_ThenVerified()
    {
        _credential.InitiateRegistrationVerification();

        _credential.VerifyRegistration();

        _credential.VerificationKeep.IsVerified.Should().BeTrue();
        _credential.Events.Last().Should().BeOfType<RegistrationVerificationVerified>();
    }

    [Fact]
    public void WhenInitiatePasswordResetAndPasswordNotSet_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();

        var result = _credential.InitiatePasswordReset();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordKeep_NoPasswordHash);
    }

    [Fact]
    public void WhenInitiatePasswordResetAndNotVerified_ThenReturnsError()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
#if TESTINGONLY
        _credential.TestingOnly_RenewVerification(Token);
#endif
        var result = _credential.InitiatePasswordReset();

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PasswordCredentialsRoot_RegistrationUnverified);
    }

    [Fact]
    public void WhenInitiatePasswordReset_ThenInitiated()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _credential.SetPasswordCredential("apassword");
        _credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();

        _credential.InitiatePasswordReset();

        _credential.Password.IsResetInitiated.Should().BeTrue();
        _credential.Events[1].Should().BeOfType<CredentialsChanged>();
        _credential.Events[2].Should().BeOfType<RegistrationChanged>();
        _credential.Events.Last().Should().BeOfType<PasswordResetInitiated>();
    }

    [Fact]
    public void WhenCompletePasswordResetWithInvalidPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(false);

        var result = _credential.CompletePasswordReset(Token, "apassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordCredentialsRoot_InvalidPassword);

        _passwordHasherService.Verify(ph => ph.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenCompletePasswordResetAndNoExistingPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);

        var result = _credential.CompletePasswordReset(Token, "apassword");

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.PasswordCredentialsRoot_NoPassword);

        _passwordHasherService.Verify(ph => ph.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhenCompletePasswordResetAndSameAsOldPassword_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _credential.SetPasswordCredential("apassword");

        var result = _credential.CompletePasswordReset(Token, "apassword");

        result.Should().BeError(ErrorCode.Validation, Resources.PasswordCredentialsRoot_DuplicatePassword);

        _passwordHasherService.Verify(ph => ph.VerifyPassword("apassword", "apasswordhash"));
    }

    [Fact]
    public void WhenCompletePasswordResetAndExpired_ThenReturnsError()
    {
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        _credential.SetPasswordCredential("apassword");
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();
#if TESTINGONLY
        _credential.TestingOnly_ExpirePasswordResetVerification();
#endif
        var result = _credential.CompletePasswordReset("atoken", "apassword");

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.PasswordCredentialsRoot_PasswordResetTokenExpired);
    }

    [Fact]
    public void WhenCompletePasswordResetAndCredentialsLocked_ThenResetsPasswordAndUnlocks()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _credential.SetPasswordCredential("apassword");
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
#if TESTINGONLY
        _credential.TestingOnly_LockAccount("awrongpassword");
#endif
        _credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();
        _credential.InitiatePasswordReset();
        _passwordHasherService.Setup(es => es.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        _credential.CompletePasswordReset(_credential.Password.ResetToken, "anewpassword");

        _passwordHasherService.Verify(ph => ph.ValidatePassword("apassword", true));
        _passwordHasherService.Verify(ph => ph.ValidatePassword("anewpassword", false));
        _credential.Login.IsLocked.Should().BeFalse();
        _credential.Login.ToggledLocked.Should().BeTrue();
        _credential.Events[12].Should().BeOfType<PasswordResetCompleted>();
        _credential.Events.Last().Should().BeOfType<AccountUnlocked>();
    }

    [Fact]
    public void WhenCompletePasswordReset_ThenResetsPassword()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _passwordHasherService.Setup(phs => phs.ValidatePassword(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(true);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        _credential.SetPasswordCredential("apassword");
        _credential.SetPasswordCredential("apassword");
        _credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();
        _credential.InitiatePasswordReset();

        _credential.CompletePasswordReset(_credential.Password.ResetToken, "anewpassword");
        _passwordHasherService.Verify(ph => ph.ValidatePassword("apassword", true));
        _passwordHasherService.Verify(ph => ph.ValidatePassword("anewpassword", false));
    }

    [Fact]
    public void WhenEnsureInvariantsAndRegisteredButEmailNotUnique_ThenReturnsErrors()
    {
        _credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("adisplayname").Value);
        _emailAddressService.Setup(eas => eas.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .ReturnsAsync(false);

        var result = _credential.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.PasswordCredentialsRoot_EmailNotUnique);
    }

    [Fact]
    public void WhenEnsureInvariantsAndInitiatingPasswordResetButUnRegistered_ThenReturnsErrors()
    {
        _tokensService.Setup(ts => ts.CreatePasswordResetToken())
            .Returns(Token);
        _credential.SetPasswordCredential("apassword");
        _credential.InitiateRegistrationVerification();
        _credential.VerifyRegistration();
        _credential.InitiatePasswordReset();

#if TESTINGONLY
        _credential.TestingOnly_Unregister();
#endif

        var result = _credential.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.PasswordCredentialsRoot_PasswordInitiatedWithoutRegistration);
    }
}