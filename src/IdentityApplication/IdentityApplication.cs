using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using IdentityApplication.ApplicationServices;

namespace IdentityApplication;

public class IdentityApplication : IIdentityApplication
{
    private readonly IPersonCredentialsService _personCredentialsService;

    public IdentityApplication(IPersonCredentialsService personCredentialsService)
    {
        _personCredentialsService = personCredentialsService;
    }

    public async Task<Result<Identity, Error>> GetIdentityAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var isMfaEnabled = false;
        var hasCredentials = false;
        var receivedCredentials =
            await _personCredentialsService.GetCredentialsPrivateAsync(caller, cancellationToken);
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