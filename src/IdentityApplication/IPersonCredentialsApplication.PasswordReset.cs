using Application.Interfaces;
using Common;

namespace IdentityApplication;

public partial interface IPersonCredentialsApplication
{
    Task<Result<Error>> CompletePasswordResetAsync(ICallerContext caller, string token, string password,
        CancellationToken cancellationToken);

    Task<Result<Error>> InitiatePasswordResetAsync(ICallerContext caller, string emailAddress,
        CancellationToken cancellationToken);

    Task<Result<Error>> ResendPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);

    Task<Result<Error>> VerifyPasswordResetAsync(ICallerContext caller, string token,
        CancellationToken cancellationToken);
}