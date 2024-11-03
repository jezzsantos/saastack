using Common.Configuration;
using Domain.Services.Shared;
using FluentAssertions;
using IdentityInfrastructure.ApplicationServices;
using Moq;
using Xunit;

namespace IdentityInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class MfaServiceSpec
{
    private readonly MfaService _mfaService;
    private readonly Mock<ITokensService> _tokensService;

    public MfaServiceSpec()
    {
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(x => x.Platform.GetString(MfaService.PlatformNameSettingName, It.IsAny<string>()))
            .Returns("anissuer");
        _tokensService = new Mock<ITokensService>();
        _mfaService = new MfaService(settings.Object, _tokensService.Object);
    }

    [Fact]
    public void WhenGenerateOobSecret_ThenCreatesRandomSixDigits()
    {
        var result = _mfaService.GenerateOobSecret();

        result.Length.Should().Be(6);
        result.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void WhenGenerateOtpSecret_ThenReturnsRandomBase32()
    {
        _tokensService.Setup(x => x.GenerateRandomToken())
            .Returns("=/\\_-+1234567890abcdefghijklmnopqrstuvwxyz");
        var result = _mfaService.GenerateOtpSecret();

        result.Length.Should().Be(MfaService.OtpSecretLength);
        result.Should().MatchRegex(@"^[A-Z2-7]{16}$");
    }

    [Fact]
    public void WhenGenerateOtpBarcodeUri_ThenReturnsUri()
    {
        var result = _mfaService.GenerateOtpBarcodeUri("auserid", "asecret");

        result.Should()
            .Be(
                $"otpauth://totp/anissuer:auserid?"
                + $"secret=asecret"
                + $"&issuer=anissuer"
                + $"&algorithm={MfaService.HashMode.ToString().ToUpper()}"
                + $"&digits={MfaService.SizeOfCode}"
                + $"&period={MfaService.CodeRenewPeriod}");
    }

    [Fact]
    public void WhenVerifyTotpAndWrongConfirmationCode_ThenReturnsFail()
    {
        const string confirmationCode = "111111";
        var result = _mfaService.VerifyTotp("asecret", new List<long>(), confirmationCode);

        result.IsValid.Should().BeFalse();
        result.TimeStepMatched.Should().BeNull();
    }

    [Fact]
    public void WhenVerifyTotpAndCorrectConfirmationCode_ThenReturnsSuccess()
    {
#if TESTINGONLY
        var confirmationCode = MfaService.CalculateTotp("asecret");
#else
        var confirmationCode = "123456";
#endif

        var result = _mfaService.VerifyTotp("asecret", new List<long>(), confirmationCode);

        result.IsValid.Should().BeTrue();
        result.TimeStepMatched.Should().NotBeNull();
    }

    [Fact]
    public void WhenVerifyTotpAndCorrectConfirmationCodeAndReplayed_ThenReturnsFail()
    {
#if TESTINGONLY
        var confirmationCode = MfaService.CalculateTotp("asecret");
#else
        var confirmationCode = "123456";
#endif
        var previous = _mfaService.VerifyTotp("asecret", new List<long>(), confirmationCode);

        previous.IsValid.Should().BeTrue();

        var previousTimeSteps = new List<long> { previous.TimeStepMatched!.Value };

        var result = _mfaService.VerifyTotp("asecret", previousTimeSteps, confirmationCode);

        result.IsValid.Should().BeFalse();
        result.TimeStepMatched.Should().BeNull();
    }
}