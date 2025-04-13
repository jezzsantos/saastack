using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IPersonCredentialsService
{
    Task<Result<PersonCredential, Error>> GetCredentialsPrivateAsync(ICallerContext caller,
        CancellationToken cancellationToken);
}