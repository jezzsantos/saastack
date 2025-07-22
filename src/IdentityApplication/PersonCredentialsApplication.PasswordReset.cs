using Application.Interfaces;
using Common;

namespace IdentityApplication;

partial class PersonCredentialsApplication
{
    public async Task<Result<Error>> CompletePasswordResetAsync(ICallerContext caller, string token, string password,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.CompletePasswordResetAsync(caller, token, password,
            cancellationToken);
    }

    public async Task<Result<Error>> InitiatePasswordResetAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.InitiatePasswordResetAsync(caller, emailAddress,
            cancellationToken);
    }

    public async Task<Result<Error>> ResendPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.ResendPasswordResetAsync(caller, token,
            cancellationToken);
    }

    public async Task<Result<Error>> VerifyPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken)
    {
        return await _identityServerProvider.CredentialsService.VerifyPasswordResetAsync(caller, token,
            cancellationToken);
    }
}