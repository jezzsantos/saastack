using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

public class PersonCredentialsService : IPersonCredentialsService
{
    private readonly IPersonCredentialsApplication _personCredentialsApplication;

    public PersonCredentialsService(IPersonCredentialsApplication personCredentialsApplication)
    {
        _personCredentialsApplication = personCredentialsApplication;
    }

    public Task<Result<PersonCredential, Error>> GetCredentialsPrivateAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return _personCredentialsApplication.GetPersonCredentialAsync(caller, cancellationToken);
    }
}