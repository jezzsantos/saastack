using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public interface IPasswordCredentialsService
{
    Task<Result<PasswordCredential, Error>> GetCredentialsPrivateAsync(ICallerContext caller,
        CancellationToken cancellationToken);
}