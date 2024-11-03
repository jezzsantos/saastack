using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using Domain.Shared.Identities;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class MfaAuthenticatorSpec
{
    private readonly MfaAuthenticator _authenticator;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IMfaService> _mfaService;

    public MfaAuthenticatorSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _mfaService = new Mock<IMfaService>();
        _mfaService.Setup(ms => ms.GenerateOobCode())
            .Returns("anoobcode");
        _mfaService.Setup(ms => ms.GenerateOobSecret())
            .Returns("anoobsecret");
        _mfaService.Setup(ms => ms.GenerateOtpSecret())
            .Returns("anotpsecret");
        _mfaService.Setup(ms => ms.GenerateOtpBarcodeUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("anotpbarcodeuri");
        _mfaService.Setup(ms => ms.GetTotpMaxTimeSteps())
            .Returns(3);

        _authenticator = MfaAuthenticator.Create(recorder.Object, idFactory.Object,
            _encryptionService.Object, _mfaService.Object, _ => Result.Ok).Value;
    }

    [Fact]
    public void WhenAdded_ThenAdded()
    {
        CreateAuthenticator(MfaAuthenticatorType.None);

        _authenticator.RootId.Should().BeSome("anid".ToId());
        _authenticator.UserId.Should().BeSome("auserid".ToId());
        _authenticator.IsActive.Should().BeFalse();
        _authenticator.State.Should().Be(MfaAuthenticatorState.Created);
        _authenticator.Type.Should().Be(MfaAuthenticatorType.None);
        _authenticator.OobCode.Should().BeNone();
        _authenticator.Secret.Should().BeNone();
        _authenticator.BarCodeUri.Should().BeNone();
        _authenticator.OobChannelValue.Should().BeNone();
        _authenticator.VerifiedState.Should().BeNone();
        _authenticator.HasBeenConfirmed.Should().BeFalse();
    }

    [Fact]
    public void WhenAssociateAndAlreadyConfirmed_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.None);
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.MfaAuthenticator_ConfirmAssociation_NotAssociated);
    }

    [Fact]
    public void WhenAssociateForNone_ThenThrows()
    {
        CreateAuthenticator(MfaAuthenticatorType.None);

        _authenticator.Invoking(x => x.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
                Optional<EmailAddress>.None, Optional<string>.None))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WhenAssociateForRecoveryCodesAndNone_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);

        var result = _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_NoRecoveryCodes);
    }

    [Fact]
    public void WhenAssociateForRecoveryCodesAndCodes_ThenAssociates()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);

        var result = _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, "arecoverycode");

        result.Should().BeSuccess();
        _authenticator.OobCode.Should().BeNone();
        _authenticator.OobChannelValue.Should().BeNone();
        _authenticator.Secret.Should().BeSome("arecoverycode");
        _encryptionService.Verify(es => es.Encrypt("arecoverycode"));
    }

    [Fact]
    public void WhenAssociateForOobSmsAndNoPhoneNumber_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);

        var result = _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_OobSms_NoPhoneNumber);
    }

    [Fact]
    public void WhenAssociateForOobSmsAndPhoneNumber_ThenAssociates()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);

        var result = _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        result.Should().BeSuccess();
        _authenticator.IsActive.Should().BeFalse();
        _authenticator.OobCode.Should().BeSome("anoobcode");
        _authenticator.OobChannelValue.Should().BeSome("+6498876986");
        _authenticator.Secret.Should().BeSome("anoobsecret");
        _mfaService.Verify(ms => ms.GenerateOobSecret());
        _mfaService.Verify(ms => ms.GenerateOobCode());
        _encryptionService.Verify(es => es.Encrypt("anoobsecret"));
    }

    [Fact]
    public void WhenAssociateForOobEmailAndNoEmailAddress_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);

        var result = _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_OobEmail_NoEmailAddress);
    }

    [Fact]
    public void WhenAssociateForOobEmailAndPhoneNumber_ThenAssociates()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);

        var result = _authenticator.Associate(Optional<PhoneNumber>.None,
            EmailAddress.Create("auser@company.com").Value, Optional<EmailAddress>.None, Optional<string>.None);

        result.Should().BeSuccess();
        _authenticator.IsActive.Should().BeFalse();
        _authenticator.OobCode.Should().BeSome("anoobcode");
        _authenticator.OobChannelValue.Should().BeSome("auser@company.com");
        _authenticator.Secret.Should().BeSome("anoobsecret");
        _mfaService.Verify(ms => ms.GenerateOobSecret());
        _mfaService.Verify(ms => ms.GenerateOobCode());
        _encryptionService.Verify(es => es.Encrypt("anoobsecret"));
    }

    [Fact]
    public void WhenAssociateForTotpAuthenticatorAndNoUsername_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);

        var result = _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_OtpAuthenticator_NoUsername);
    }

    [Fact]
    public void WhenAssociateForTotpAuthenticator_ThenAssociates()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);

        var result = _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, Optional<string>.None);

        result.Should().BeSuccess();
        _authenticator.IsActive.Should().BeFalse();
        _authenticator.OobCode.Should().BeNone();
        _authenticator.OobChannelValue.Should().BeNone();
        _authenticator.BarCodeUri.Should().BeSome("anotpbarcodeuri");
        _authenticator.Secret.Should().BeSome("anotpsecret");
        _encryptionService.Verify(es => es.Encrypt("anotpsecret"));
        _mfaService.Verify(ms => ms.GenerateOtpSecret());
        _mfaService.Verify(ms => ms.GenerateOtpBarcodeUri("auser@company.com", "anotpsecret"));
    }

    [Fact]
    public void WhenConfirmAssociationAndIsConfirmed_ThenReturnsError()
    {
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.ConfirmAssociation("oobCode", "aconfirmationcode");

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.MfaAuthenticator_ConfirmAssociation_NotAssociated);
    }

    [Fact]
    public void WhenConfirmAssociationForNoneAuthenticator_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.None);
        _authenticator.RaiseChangeEvent(Events.PasswordCredentials.MfaAuthenticatorAssociated("anid".ToId(),
            _authenticator, "anoobcode", Optional<string>.None, Optional<string>.None,
            Optional<string>.None));

        var result = _authenticator.ConfirmAssociation("anotheroobcode", "aconfirmationcode");

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.MfaAuthenticator_ConfirmAssociation_InvalidType);
    }

    [Fact]
    public void WhenConfirmAssociationForRecoveryCodesAuthenticator_ThenConfirms()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            "arecoverycode");

        var result = _authenticator.ConfirmAssociation("anotheroobcode", "aconfirmationcode");

        result.Should().BeSuccess();
        _authenticator.IsActive.Should().BeTrue();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("arecoverycode");
        _authenticator.VerifiedState.Should().BeNone();
    }

    [Fact]
    public void WhenConfirmAssociationForOobSmsAndOobCodeDoesNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
        _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        var result = _authenticator.ConfirmAssociation("anotheroobcode", "aconfirmationcode");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenConfirmAssociationForOobSmsAndConfirmationCodeNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
        _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        var result = _authenticator.ConfirmAssociation("anoobcode", "anothersecret");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenConfirmAssociationForOobSms_ThenConfirms()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
        _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);

        var result = _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _authenticator.IsActive.Should().BeTrue();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anoobsecret");
        _authenticator.VerifiedState.Should().BeNone();
    }

    [Fact]
    public void WhenConfirmAssociationForOobEmailAndOobCodeDoesNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
        _authenticator.Associate(Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None, Optional<string>.None);

        var result = _authenticator.ConfirmAssociation("anotheroobcode", "aconfirmationcode");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenConfirmAssociationForOobEmailAndConfirmationCodeNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
        _authenticator.Associate(Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None, Optional<string>.None);

        var result = _authenticator.ConfirmAssociation("anoobcode", "anothersecret");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenConfirmAssociationForOobEmail_ThenConfirms()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
        _authenticator.Associate(Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None, Optional<string>.None);

        var result = _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _authenticator.IsActive.Should().BeTrue();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anoobsecret");
        _authenticator.VerifiedState.Should().BeNone();
    }

    [Fact]
    public void WhenConfirmAssociationForTotpAndNotMatchCode_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, Optional<string>.None);
        _mfaService.Setup(ms => ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()))
            .Returns(TotpResult.Invalid);

        var result = _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list => list.Count == 0), "aconfirmationcode"));
    }

    [Fact]
    public void WhenConfirmAssociationForTotp_ThenConfirms()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, Optional<string>.None);
        _mfaService.Setup(ms => ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()))
            .Returns(TotpResult.Valid(1));

        var result = _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");

        result.Should().BeSuccess();
        _authenticator.IsActive.Should().BeTrue();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anotpsecret");
        _authenticator.VerifiedState.Should().Be("1");
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list => list.Count == 0), "aconfirmationcode"));
        _encryptionService.Verify(es => es.Encrypt("1"));
    }

    [Fact]
    public void WhenChallengeAndNotConfirmed_ThenReturnsError()
    {
        var result = _authenticator.Challenge();

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.MfaAuthenticator_Challenge_NotConfirmed);
    }

    [Fact]
    public void WhenChallengeForNoneThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.None);
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.Challenge();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.MfaAuthenticator_Challenge_InvalidAuthenticator);
    }

    [Fact]
    public void WhenChallengeForRecoveryCodes_ThenDoesNothing()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.Challenge();

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenChallengeForTotpAuthenticator_ThenDoesNothing()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.Challenge();

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenChallengeForOobSmsAndMissingPhoneNumber_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.Challenge();

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_OobSms_NoPhoneNumber);
    }

    [Fact]
    public void WhenChallengeForOobSms_ThenChallenges()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
        _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);
        _mfaService.Setup(ms => ms.GenerateOobCode())
            .Returns("anewoobcode");
        _mfaService.Setup(ms => ms.GenerateOobSecret())
            .Returns("anewoobsecret");
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Challenge();

        result.Should().BeSuccess();
        _authenticator.OobCode.Should().Be("anewoobcode");
        _authenticator.BarCodeUri.Should().BeNone();
        _authenticator.Secret.Should().BeSome("anewoobsecret");
        _authenticator.OobChannelValue.Should().BeSome("+6498876986");
        _mfaService.Verify(ms => ms.GenerateOobCode());
    }

    [Fact]
    public void WhenChallengeForOobEmailAndMissingEmailAddress_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.Challenge();

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.MfaAuthenticator_Associate_OobEmail_NoEmailAddress);
    }

    [Fact]
    public void WhenChallengeForOobEmail_ThenChallenges()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
        _authenticator.Associate(Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None, Optional<string>.None);
        _mfaService.Setup(ms => ms.GenerateOobCode())
            .Returns("anewoobcode");
        _mfaService.Setup(ms => ms.GenerateOobSecret())
            .Returns("anewoobsecret");
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Challenge();

        result.Should().BeSuccess();
        _authenticator.OobCode.Should().Be("anewoobcode");
        _authenticator.BarCodeUri.Should().BeNone();
        _authenticator.Secret.Should().BeSome("anewoobsecret");
        _authenticator.OobChannelValue.Should().BeSome("auser@company.com");
        _mfaService.Verify(ms => ms.GenerateOobCode());
    }

    [Fact]
    public void WhenVerifyAndNotConfirmedAndNotChallenged_ThenReturnsError()
    {
        var result = _authenticator.Verify(Optional<string>.None, Optional<string>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.MfaAuthenticator_Verify_NotVerifiable);
    }

    [Fact]
    public void WhenVerifyForNoneAuthenticator_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.None);
#if TESTINGONLY
        _authenticator.TestingOnly_Confirm();
#endif

        var result = _authenticator.Verify(Optional<string>.None, "aconfirmationcode");

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.MfaAuthenticator_Verify_InvalidType);
    }

    [Fact]
    public void WhenVerifyForRecoveryCodesAndCodeDoesNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            "acode");
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");

        var result = _authenticator.Verify(Optional<string>.None, "anothercode");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenVerifyForRecoveryCodes_ThenVerifies()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            "acode");
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");

        var result = _authenticator.Verify(Optional<string>.None, "acode");

        result.Should().BeSuccess();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("acode");
        _authenticator.VerifiedState.Should().BeSome("acode");
        _encryptionService.Verify(es => es.Encrypt("acode"));
    }

    [Fact]
    public void WhenVerifyForRecoveryCodesAgain_ThenVerifiesAndAccumulatesUsedCodes()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            "acode1;acode2");
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");
        _authenticator.Verify(Optional<string>.None, "acode1");

        var result = _authenticator.Verify(Optional<string>.None, "acode2");

        result.Should().BeSuccess();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("acode1;acode2");
        _authenticator.VerifiedState.Should().BeSome("acode1;acode2");
        _authenticator.VerifiedState.Should().BeSome("acode1;acode2");
    }

    [Fact]
    public void WhenVerifyForRecoveryCodesAgainWithUsedCode_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            "acode1");
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");
        _authenticator.Verify(Optional<string>.None, "acode1");

        var result = _authenticator.Verify(Optional<string>.None, "acode1");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenVerifyForRecoveryCodesAgainWithAllUsedCodes_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.RecoveryCodes);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None, Optional<EmailAddress>.None,
            "acode1;acode2;acode3");
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");
        _authenticator.Verify(Optional<string>.None, "acode1");
        _authenticator.Verify(Optional<string>.None, "acode2");
        _authenticator.Verify(Optional<string>.None, "acode3");

        var result = _authenticator.Verify(Optional<string>.None, "acode2");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenVerifyForOobSmsAndOobCodeDoesNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
        _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Verify("anotheroobcode", "anoobsecret");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenVerifyForOobSmsAndConfirmationCodeNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
        _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Verify("anoobcode", "anothersecret");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenVerifyForOobSms_ThenVerifies()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobSms);
        _authenticator.Associate(PhoneNumber.Create("+6498876986").Value, Optional<EmailAddress>.None,
            Optional<EmailAddress>.None, Optional<string>.None);
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Verify("anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anoobsecret");
        _authenticator.VerifiedState.Should().BeNone();
    }

    [Fact]
    public void WhenVerifyForOobEmailAndOobCodeDoesNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
        _authenticator.Associate(Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None, Optional<string>.None);
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Verify("anotheroobcode", "anoobsecret");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenVerifyForOobEmailAndConfirmationCodeNotMatch_ThenReturnsError()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
        _authenticator.Associate(Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None, Optional<string>.None);
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Verify("anoobcode", "anothersecret");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenVerifyForOobEmail_ThenVerifies()
    {
        CreateAuthenticator(MfaAuthenticatorType.OobEmail);
        _authenticator.Associate(Optional<PhoneNumber>.None, EmailAddress.Create("auser@company.com").Value,
            Optional<EmailAddress>.None, Optional<string>.None);
        _authenticator.ConfirmAssociation("anoobcode", "anoobsecret");

        var result = _authenticator.Verify("anoobcode", "anoobsecret");

        result.Should().BeSuccess();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anoobsecret");
        _authenticator.VerifiedState.Should().BeNone();
    }

    [Fact]
    public void WhenVerifyForTotp_ThenVerifies()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, Optional<string>.None);
        _mfaService.SetupSequence(ms =>
                ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()))
            .Returns(TotpResult.Valid(1))
            .Returns(TotpResult.Valid(2));
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");

        var result = _authenticator.Verify(Optional<string>.None, "aconfirmationcode");

        result.Should().BeSuccess();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anotpsecret");
        _authenticator.VerifiedState.Should().BeSome("1;2");
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list => list.Count == 0), "aconfirmationcode"));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list =>
                list.Count == 1
                && list[0] == 1
            ), "aconfirmationcode"));
        _encryptionService.Verify(es => es.Encrypt("1;2"));
    }

    [Fact]
    public void WhenVerifyForTotpAgain_ThenVerifiesAndAccumulatesUsedTimeSteps()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, Optional<string>.None);
        _mfaService.SetupSequence(ms =>
                ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()))
            .Returns(TotpResult.Valid(1))
            .Returns(TotpResult.Valid(2))
            .Returns(TotpResult.Valid(3));
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");
        _authenticator.Verify(Optional<string>.None, "aconfirmationcode");

        var result = _authenticator.Verify(Optional<string>.None, "aconfirmationcode");

        result.Should().BeSuccess();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anotpsecret");
        _authenticator.VerifiedState.Should().BeSome("1;2;3");
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list => list.Count == 0), "aconfirmationcode"));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list =>
                list.Count == 1
                && list[0] == 1
            ), "aconfirmationcode"));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list =>
                list.Count == 2
                && list[0] == 1
                && list[1] == 2
            ), "aconfirmationcode"));
        _encryptionService.Verify(es => es.Encrypt("1"));
        _encryptionService.Verify(es => es.Encrypt("1;2"));
        _encryptionService.Verify(es => es.Encrypt("1;2;3"));
    }

    [Fact]
    public void WhenVerifyForTotpAgainManyTimes_ThenVerifiesAndAccumulatesOnlyLatestUsedTimeSteps()
    {
        CreateAuthenticator(MfaAuthenticatorType.TotpAuthenticator);
        _authenticator.Associate(Optional<PhoneNumber>.None, Optional<EmailAddress>.None,
            EmailAddress.Create("auser@company.com").Value, Optional<string>.None);
        _mfaService.SetupSequence(ms =>
                ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()))
            .Returns(TotpResult.Valid(1))
            .Returns(TotpResult.Valid(2))
            .Returns(TotpResult.Valid(3))
            .Returns(TotpResult.Valid(4))
            .Returns(TotpResult.Valid(5));
        _mfaService.Setup(ms => ms.GetTotpMaxTimeSteps())
            .Returns(2);
        _authenticator.ConfirmAssociation(Optional<string>.None, "aconfirmationcode");
        _authenticator.Verify(Optional<string>.None, "aconfirmationcode");
        _authenticator.Verify(Optional<string>.None, "aconfirmationcode");
        _authenticator.Verify(Optional<string>.None, "aconfirmationcode");

        var result = _authenticator.Verify(Optional<string>.None, "aconfirmationcode");

        result.Should().BeSuccess();
        _authenticator.HasBeenConfirmed.Should().BeTrue();
        _authenticator.Secret.Should().BeSome("anotpsecret");
        _authenticator.VerifiedState.Should().BeSome("4;5");
        _mfaService.Verify(ms => ms.VerifyTotp(It.IsAny<string>(), It.IsAny<IReadOnlyList<long>>(), It.IsAny<string>()),
            Times.Exactly(5));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list => list.Count == 0), "aconfirmationcode"));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list =>
                list.Count == 1
                && list[0] == 1
            ), "aconfirmationcode"));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list =>
                list.Count == 2
                && list[0] == 1
                && list[1] == 2
            ), "aconfirmationcode"));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list =>
                list.Count == 2
                && list[0] == 2
                && list[1] == 3
            ), "aconfirmationcode"));
        _mfaService.Verify(ms =>
            ms.VerifyTotp("anotpsecret", It.Is<IReadOnlyList<long>>(list =>
                list.Count == 2
                && list[0] == 3
                && list[1] == 4
            ), "aconfirmationcode"));
        _encryptionService.Verify(es => es.Encrypt("1"));
        _encryptionService.Verify(es => es.Encrypt("1;2"));
        _encryptionService.Verify(es => es.Encrypt("2;3"));
        _encryptionService.Verify(es => es.Encrypt("4;5"));
    }

    private void CreateAuthenticator(MfaAuthenticatorType type)
    {
        _authenticator.RaiseChangeEvent(Events.PasswordCredentials.MfaAuthenticatorAdded("anid".ToId(),
            "auserid".ToId(), type, true));
    }
}