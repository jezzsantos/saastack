namespace IdentityDomain.DomainServices;

/// <summary>
///     Defines a service for performing MFA calculations and translations
/// </summary>
public interface IMfaService
{
    /// <summary>
    ///     Generates a random OOB code
    /// </summary>
    /// <returns></returns>
    string GenerateOobCode();

    /// <summary>
    ///     Generates a unique 6-digit code for OOB challenges
    /// </summary>
    string GenerateOobSecret();

    /// <summary>
    ///     Generates a barcode URI for configuring an authenticator app, with the given username and secret
    /// </summary>
    string GenerateOtpBarcodeUri(string username, string secret);

    /// <summary>
    ///     Creates an uppercase alphanumeric string of 16 characters.
    /// </summary>
    string GenerateOtpSecret();

    /// <summary>
    ///     Returns the number of time steps in TOTP verification that could be replayed later.
    ///     These are the number of time steps in the tolerated window that the user could have entered the code in.
    /// </summary>
    int GetTotpMaxTimeSteps();

    /// <summary>
    ///     Verifies a TOTP code against a secret.
    /// </summary>
    /// <remarks>
    ///     The caller must keep track of the previous time steps to prevent replay attacks,
    ///     and if this result is successful add the <see cref="TotpResult.TimeStepMatched" /> to the saved collection.
    /// </remarks>
    TotpResult VerifyTotp(string secret, IReadOnlyList<long> previousTimeSteps, string confirmationCode);
}

public class TotpResult
{
    public static readonly TotpResult Invalid = new(null, false);

    public TotpResult(long timeStepMatched)
    {
        TimeStepMatched = timeStepMatched;
        IsValid = true;
    }

    private TotpResult(long? timeStepMatched, bool isValid)
    {
        TimeStepMatched = timeStepMatched;
        IsValid = isValid;
    }

    public bool IsValid { get; }

    public long? TimeStepMatched { get; }

    public static TotpResult Valid(int timeStepMatched)
    {
        return new TotpResult(timeStepMatched, true);
    }
}