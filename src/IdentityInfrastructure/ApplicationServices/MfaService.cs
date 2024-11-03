using System.Security.Cryptography;
using Common.Configuration;
using Common.Extensions;
using Domain.Services.Shared;
using IdentityDomain.DomainServices;
using OtpNet;

namespace IdentityInfrastructure.ApplicationServices;

/// <summary>
///     Provides a service for performing MFA calculations and translations
/// </summary>
/// <remarks>
///     OTP Algorithms provided by <see href="https://github.com/kspearrin/Otp.NET" />.
///     TOTP Compliant with <see href="http://tools.ietf.org/html/rfc6238" />
/// </remarks>
public class MfaService : IMfaService
{
    public enum TimeStep
    {
        Now,
        Next
    }

    internal const int CodeRenewPeriod = 30; // how often the OTP code renews
    internal const OtpHashMode HashMode = OtpHashMode.Sha1; // Hash mode for the OTP code
    internal const int OtpSecretLength = 16;
    internal const string PlatformNameSettingName = "DomainServices:MfaService:IssuerName";
    internal const int SizeOfCode = 6; // Number of digits of the OTP code
    private const int DefaultVerificationWindow = 1; // number of OTP time steps that are permitted
    private readonly string _issuer; // Name that appears in the Authenticator App
    private readonly ITokensService _tokensService;

    public MfaService(IConfigurationSettings settings, ITokensService tokensService)
    {
        _tokensService = tokensService;
        _issuer = settings.Platform.GetString(PlatformNameSettingName);
    }

    public string GenerateOobCode()
    {
        return _tokensService.GenerateRandomToken();
    }

    public string GenerateOobSecret()
    {
        var random = RandomNumberGenerator.GetInt32(0, 1000000);
        return random.ToString().PadLeft(6, '0');
    }

    public string GenerateOtpBarcodeUri(string username, string secret)
    {
        var uri = new OtpUri(OtpType.Totp, secret, username, _issuer);
        return uri.ToString();
    }

    /// <summary>
    ///     Generates a new OTP secret
    /// </summary>
    /// <remarks>
    ///     This secret must be convertible to base 32, which means it must only be characters A-Z and 2-7.
    /// </remarks>
    public string GenerateOtpSecret()
    {
        var token1 = _tokensService.GenerateRandomToken();
        var token2 = _tokensService.GenerateRandomToken();
        var randomToken = $"{token1}{token2}";

        return randomToken
            .ToUpper()
            .ReplaceWith("[^A-Z2-7]", string.Empty)
            .Substring(0, OtpSecretLength);
    }

    public int GetTotpMaxTimeSteps()
    {
        return DefaultVerificationWindow + 2; // Add a couple more to be sure
    }

    public TotpResult VerifyTotp(string secret, IReadOnlyList<long> previousTimeSteps, string confirmationCode)
    {
        var totp = GetOtp(secret);
        // As per RFC6238 recommendation
        var verificationWindow = new VerificationWindow(DefaultVerificationWindow, DefaultVerificationWindow);

        var matched = totp.VerifyTotp(confirmationCode, out var timeStepMatched, verificationWindow);
        if (matched)
        {
            if (previousTimeSteps.Contains(timeStepMatched))
            {
                return TotpResult.Invalid;
            }
        }
        else
        {
            return TotpResult.Invalid;
        }

        return new TotpResult(timeStepMatched);
    }

#if TESTINGONLY
    public static string CalculateTotp(string secret, TimeStep timeStep = TimeStep.Now)
    {
        var totp = GetOtp(secret);
        var time = timeStep == TimeStep.Now
            ? DateTime.UtcNow
            : DateTime.UtcNow.AddSeconds(CodeRenewPeriod);
        return totp.ComputeTotp(time);
    }
#endif

    private static Totp GetOtp(string secret)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes, mode: HashMode, step: CodeRenewPeriod, totpSize: SizeOfCode);
        return totp;
    }
}