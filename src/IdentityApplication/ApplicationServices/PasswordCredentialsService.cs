using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public class PasswordCredentialsService : IPasswordCredentialsService
{
    private readonly IPasswordCredentialsApplication _passwordCredentialsApplication;

    public PasswordCredentialsService(IPasswordCredentialsApplication passwordCredentialsApplication)
    {
        _passwordCredentialsApplication = passwordCredentialsApplication;
    }

    public Task<Result<PasswordCredential, Error>> GetCredentialsPrivateAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return _passwordCredentialsApplication.GetPasswordCredentialAsync(caller, cancellationToken);
    }
}