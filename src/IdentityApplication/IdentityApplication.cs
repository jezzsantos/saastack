using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;

namespace IdentityApplication;

public class IdentityApplication : IIdentityApplication
{
    private readonly IIdentityServerProvider _identityServerProvider;

    public IdentityApplication(IIdentityServerProvider identityServerProvider)
    {
        _identityServerProvider = identityServerProvider;
    }

    public async Task<Result<Identity, Error>> GetIdentityAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        var isMfaEnabled = false;
        var hasCredentials = false;
        var receivedCredentials =
            await _identityServerProvider.CredentialsService.GetPersonCredentialForUserAsync(caller,
                caller.ToCallerId(), cancellationToken);
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