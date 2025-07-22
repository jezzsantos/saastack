using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

partial class PersonCredentialsApplication
{
    public async Task<Result<CredentialMfaAuthenticatorAssociation, Error>> AssociateMfaAuthenticatorAsync(
        ICallerContext caller, string? mfaToken, CredentialMfaAuthenticatorType authenticatorType, string? phoneNumber,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.AssociateMfaAuthenticatorForUserAsync(caller,
            caller.CallerId, mfaToken, authenticatorType, phoneNumber, cancellationToken);
    }

    public async Task<Result<CredentialMfaAuthenticatorChallenge, Error>> ChallengeMfaAuthenticatorAsync(
        ICallerContext caller, string mfaToken, string authenticatorId, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.ChallengeMfaAuthenticatorForUserAsync(caller,
            caller.CallerId, mfaToken, authenticatorId, cancellationToken);
    }

    public async Task<Result<PersonCredential, Error>> ChangeMfaAsync(ICallerContext caller, bool isEnabled,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.ChangeMfaForUserAsync(caller, caller.CallerId,
            isEnabled, cancellationToken);
    }

    public async Task<Result<CredentialMfaAuthenticatorConfirmation, Error>> ConfirmMfaAuthenticatorAssociationAsync(
        ICallerContext caller, string? mfaToken, CredentialMfaAuthenticatorType authenticatorType, string? oobCode,
        string confirmationCode, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.ConfirmMfaAuthenticatorAssociationForUserAsync(caller,
            caller.CallerId, mfaToken, authenticatorType, oobCode, confirmationCode, cancellationToken);
    }

    public async Task<Result<Error>> DisassociateMfaAuthenticatorAsync(ICallerContext caller, string authenticatorId,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.DisassociateMfaAuthenticatorForUserAsync(caller,
            caller.CallerId, authenticatorId, cancellationToken);
    }

    public async Task<Result<List<CredentialMfaAuthenticator>, Error>> ListMfaAuthenticatorsAsync(ICallerContext caller,
        string? mfaToken, CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.ListMfaAuthenticatorsForUserAsync(caller,
            caller.CallerId, mfaToken, cancellationToken);
    }

    public async Task<Result<PersonCredential, Error>> ResetPasswordMfaAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.ResetPasswordMfaForUserAsync(caller, userId,
            cancellationToken);
    }

    public async Task<Result<AuthenticateTokens, Error>> VerifyMfaAuthenticatorAsync(ICallerContext caller,
        string mfaToken, CredentialMfaAuthenticatorType authenticatorType, string? oobCode, string confirmationCode,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.VerifyMfaAuthenticatorForUserAsync(caller,
            caller.CallerId, mfaToken, authenticatorType, oobCode, confirmationCode, cancellationToken);
    }
}