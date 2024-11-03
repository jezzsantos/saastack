using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using IdentityApplication.ApplicationServices;

namespace IdentityApplication;

public class IdentityApplication : IIdentityApplication
{
    private readonly IPasswordCredentialsService _passwordCredentialsService;

    public IdentityApplication(IPasswordCredentialsService passwordCredentialsService)
    {
        _passwordCredentialsService = passwordCredentialsService;
    }

    public async Task<Result<Identity, Error>> GetIdentityAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var isMfaEnabled = false;
        var hasCredentials = false;
        var receivedCredentials =
            await _passwordCredentialsService.GetCredentialsPrivateAsync(caller, cancellationToken);
        if (receivedCredentials.IsFailure)
        {
            if (receivedCredentials.Error.IsNot(ErrorCode.EntityNotFound))
            {
                return receivedCredentials.Error;
            }
        }
        else
        {
            isMfaEnabled = receivedCredentials.Value.IsMfaEnabled;
            hasCredentials = true;
        }

        return new Identity
        {
            Id = caller.CallerId,
            HasCredentials = hasCredentials,
            IsMfaEnabled = isMfaEnabled
        };
    }
}