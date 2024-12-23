using Common.Configuration;
using Common.Extensions;
using Domain.Services.Shared;
using IdentityDomain.DomainServices;
using IdentityInfrastructure.ApplicationServices;

namespace IdentityInfrastructure.IntegrationTests.Stubs;

public class StubMfaService : IMfaService
{
    private readonly MfaService _mfaService;

    public StubMfaService(IConfigurationSettings settings, ITokensService tokensService)
    {
        _mfaService = new MfaService(settings, tokensService);
    }

    public string? LastOobConfirmationCode { get; private set; }

    public string? LastOtpSecret { get; private set; }

    public string GenerateOobCode()
    {
        return _mfaService.GenerateOobCode();
    }

    public string GenerateOobSecret()
    {
        var code = _mfaService.GenerateOobSecret();
        LastOobConfirmationCode = code;
        return code;
    }

    public string GenerateOtpBarcodeUri(string username, string secret)
    {
        return _mfaService.GenerateOtpBarcodeUri(username, secret);
    }

    public string GenerateOtpSecret()
    {
        var secret = _mfaService.GenerateOtpSecret();
        LastOtpSecret = secret;
        return secret;
    }

    public int GetTotpMaxTimeSteps()
    {
        return _mfaService.GetTotpMaxTimeSteps();
    }

    public TotpResult VerifyTotp(string secret, IReadOnlyList<long> previousTimeSteps, string confirmationCode)
    {
        return _mfaService.VerifyTotp(secret, previousTimeSteps, confirmationCode);
    }

    public string GetOtpCodeNow(MfaService.TimeStep timeStep = MfaService.TimeStep.Now)
    {
        if (LastOtpSecret.NotExists())
        {
            return string.Empty;
        }

#if TESTINGONLY
        var confirmationCode = MfaService.CalculateTotp(LastOtpSecret, timeStep);
#else
        var confirmationCode = "";
#endif
        return confirmationCode;
    }

    public void Reset()
    {
        LastOobConfirmationCode = null;
        LastOtpSecret = null;
    }
}